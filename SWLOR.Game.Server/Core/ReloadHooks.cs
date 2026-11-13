using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Core
{
    /// <summary>The <see cref="ReloadHookAttribute"/> methods of a build, found and run for one phase of a hot reload.</summary>
    public static class ReloadHooks
    {
        /// <summary>
        /// Every method of <paramref name="assembly"/> marked for <paramref name="phase"/>, by order, then class and method name.
        /// Throws when a marked method is not public, does not return void or does not take only the players
        /// (<c>IReadOnlyList&lt;uint&gt;</c>), so a malformed hook fails the tests instead of a reload.
        /// </summary>
        public static IReadOnlyList<MethodInfo> Discover(Assembly assembly, ReloadHookPhase phase)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            var hooks = assembly.GetTypes()
                .SelectMany(type => type.GetMethods(flags))
                .Select(method => (Method: method, Hook: method.GetCustomAttribute<ReloadHookAttribute>(false)))
                .Where(entry => entry.Hook != null && entry.Hook.Phase == phase)
                .OrderBy(entry => entry.Hook.Order)
                .ThenBy(entry => entry.Method.DeclaringType?.FullName, StringComparer.Ordinal)
                .ThenBy(entry => entry.Method.Name, StringComparer.Ordinal)
                .Select(entry => entry.Method)
                .ToArray();

            foreach (var method in hooks)
            {
                var parameters = method.GetParameters();
                if (!method.IsPublic || method.ReturnType != typeof(void) || parameters.Length != 1 ||
                    parameters[0].ParameterType != typeof(IReadOnlyList<uint>))
                {
                    throw new InvalidOperationException(
                        $"{method.DeclaringType?.FullName}.{method.Name} is a reload hook, so it must be public, return void and take the players (IReadOnlyList<uint>).");
                }
            }

            return hooks;
        }

        /// <summary>
        /// Runs every hook of <paramref name="phase"/> with <paramref name="players"/>: a static hook as it is, an instance hook on
        /// <paramref name="resolveOwner"/>'s instance of its class (the container's singleton). A hook that fails is logged and the
        /// next one runs; when any failed, this throws after the last one, naming them.
        /// </summary>
        public static void Run(Assembly assembly, ReloadHookPhase phase, Func<Type, object> resolveOwner, IReadOnlyList<uint> players)
        {
            if (resolveOwner == null)
                throw new ArgumentNullException(nameof(resolveOwner));
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            var failed = new List<string>();
            foreach (var method in Discover(assembly, phase))
            {
                var name = $"{method.DeclaringType?.FullName}.{method.Name}";
                try
                {
                    var owner = method.IsStatic
                        ? null
                        : resolveOwner(method.DeclaringType) ?? throw new InvalidOperationException($"The service container has no instance of {method.DeclaringType?.FullName}.");
                    method.Invoke(owner, new object[] { players });
                }
                catch (Exception ex)
                {
                    failed.Add(name);
                    var error = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
                    Log.Write(LogGroup.Error, $"Reload {phase.ToString().ToLowerInvariant()} hook {name} threw. " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{error.ToMessageAndCompleteStacktrace()}", true);
                }
            }

            if (failed.Count > 0)
                throw new InvalidOperationException($"Reload {phase.ToString().ToLowerInvariant()} hooks failed: " + string.Join(", ", failed) + ".");
        }
    }
}

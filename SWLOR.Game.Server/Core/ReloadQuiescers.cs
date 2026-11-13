using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Core
{
    /// <summary>The classes of a build that end work in flight when a hot reload quiesces it (<see cref="IReloadQuiescer"/>).</summary>
    public static class ReloadQuiescers
    {
        /// <summary>Every concrete class of <paramref name="assembly"/> that implements <see cref="IReloadQuiescer"/>, by full name.</summary>
        public static IReadOnlyList<Type> Discover(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            return assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(IReloadQuiescer).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Calls <see cref="IReloadQuiescer.QuiesceForReload"/> on <paramref name="resolveOwner"/>'s instance (the container's
        /// singleton) of each class <see cref="Discover"/> finds. A class that fails is logged and the next one runs; when any failed,
        /// this throws after the last one, naming them.
        /// </summary>
        public static void Run(Assembly assembly, Func<Type, object> resolveOwner, IReadOnlyList<uint> players)
        {
            if (resolveOwner == null)
                throw new ArgumentNullException(nameof(resolveOwner));
            if (players == null)
                throw new ArgumentNullException(nameof(players));

            var failed = new List<string>();
            foreach (var type in Discover(assembly))
            {
                try
                {
                    var owner = resolveOwner(type) as IReloadQuiescer
                                ?? throw new InvalidOperationException($"The service container has no instance of {type.FullName}.");
                    owner.QuiesceForReload(players);
                }
                catch (Exception ex)
                {
                    failed.Add(type.FullName);
                    Log.Write(LogGroup.Error, $"Reload could not end the work in flight of {type.FullName}. " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
            }

            if (failed.Count > 0)
                throw new InvalidOperationException("Reload could not end the work in flight of " + string.Join(", ", failed) + ".");
        }
    }
}

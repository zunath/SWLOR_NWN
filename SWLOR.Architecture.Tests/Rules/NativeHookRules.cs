using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The rules of the native sinks (P4-9). The native hooks are installed once into the engine and must outlive every game
/// build, so each hook's managed body sits behind a sink interface of <c>SWLOR.Game.Contracts</c> that the build
/// implements:
/// <list type="bullet">
/// <item>a native hook (a method marked <c>[UnmanagedCallersOnly]</c>) may only forward to its sink: its body calls
/// nothing but methods of the sink interfaces (no helper of its own class, no function pointer, no framework call);</item>
/// <item>a type that implements a sink interface lives in the sink namespace (<c>NativeSinks</c> of the server);</item>
/// <item>the contracts assembly references nothing of the game: only the framework, the hosting contracts and Redis.</item>
/// </list>
/// </summary>
internal static class NativeHookRules
{
    /// <param name="facts">The hooks of the assembly.</param>
    /// <param name="hookNamespace">The namespace of the hook files. Other entry points marked <c>[UnmanagedCallersOnly]</c> (the
    /// engine callbacks of the bootstrapper) are not hooks of an engine function and are left out.</param>
    public static IReadOnlyList<string> HookBodyViolations(NativeHookFacts facts, string hookNamespace)
    {
        var problems = new List<string>();
        foreach (var hook in facts.Hooks.Where(hook => hook.Owner.Namespace == hookNamespace))
        {
            var strays = hook.Operations.Where(operation => !operation.IsSinkCall).Select(operation => operation.Description).Distinct().ToList();
            if (strays.Count > 0)
            {
                problems.Add($"{hook.Owner}::{hook.Method} calls {string.Join(", ", strays.OrderBy(call => call, StringComparer.Ordinal))}: " +
                             "a native hook only forwards to its sink, so its body may call nothing but a method of a sink interface.");
            }
            else if (!hook.Operations.Any(operation => operation.IsSinkCall))
            {
                problems.Add($"{hook.Owner}::{hook.Method} calls no sink: a native hook forwards to the sink of its hook file.");
            }
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    /// <param name="facts">The sink implementers of the assembly.</param>
    /// <param name="sinkHome">The namespace every sink implementation must be in.</param>
    public static IReadOnlyList<string> SinkLocationViolations(NativeHookFacts facts, string sinkHome)
    {
        return facts.Implementers
            .Where(implementer => implementer.Type.Namespace != sinkHome)
            .Select(implementer => $"{implementer.Type} implements {implementer.Interface} but is not in {sinkHome}: sinks live in the NativeSinks folder.")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    /// <param name="referencedAssemblies">The simple names of the assemblies the contracts assembly references.</param>
    /// <param name="allowed">The names it may reference besides the framework (<c>System.*</c>, <c>netstandard</c>, <c>mscorlib</c>).</param>
    public static IReadOnlyList<string> ContractsReferenceViolations(IEnumerable<string> referencedAssemblies, IReadOnlyCollection<string> allowed)
    {
        return referencedAssemblies
            .Where(name => !IsFramework(name) && !allowed.Contains(name))
            .Select(name => $"The contracts assembly references {name}: it may reference only the framework and {string.Join(", ", allowed.OrderBy(a => a, StringComparer.Ordinal))}, never anything of the game.")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsFramework(string name) =>
        name == "System" || name.StartsWith("System.", StringComparison.Ordinal) || name == "netstandard" || name == "mscorlib";
}

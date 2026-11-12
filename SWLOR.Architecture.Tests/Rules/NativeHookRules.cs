using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The rules of the native hooks (P4-9, P4-11). NWNX calls an installed hook for the life of the process, so the hook
/// entry points (the trampolines) live in <c>SWLOR.Game.NativeHooks</c>, which outlives every game build, and each hook's
/// managed body sits behind a sink interface of <c>SWLOR.Game.Contracts</c> that the build implements and binds:
/// <list type="bullet">
/// <item>a trampoline (a method marked <c>[UnmanagedCallersOnly]</c>) reads its bound sink from a <c>volatile</c> static
/// field and either forwards to it or, when none is bound, runs the engine function through its function pointer: its body
/// calls nothing but methods of the sink interfaces and function pointers (no helper of its own class, no framework
/// call), and calls both;</item>
/// <item>the game assembly holds no hook entry point (only the native entry's NWNX callbacks of <c>Core</c>);</item>
/// <item>a type that implements a sink interface lives in the sink namespace (<c>NativeSinks</c> of the server);</item>
/// <item>the contracts and the hooks assemblies reference nothing of the game: only the framework and what they are
/// allowed.</item>
/// </list>
/// </summary>
internal static class NativeHookRules
{
    /// <param name="facts">The hooks of the assembly.</param>
    /// <param name="hookNamespace">The namespace of the trampolines.</param>
    public static IReadOnlyList<string> TrampolineViolations(NativeHookFacts facts, string hookNamespace)
    {
        var problems = new List<string>();
        foreach (var hook in facts.Hooks.Where(hook => hook.Owner.Namespace == hookNamespace))
        {
            var name = $"{hook.Owner}::{hook.Method}";
            var strays = hook.Operations
                .Where(operation => !operation.IsSinkCall && !operation.IsFunctionPointerCall)
                .Select(operation => operation.Description)
                .Distinct()
                .ToList();
            if (strays.Count > 0)
            {
                problems.Add($"{name} calls {string.Join(", ", strays.OrderBy(call => call, StringComparer.Ordinal))}: " +
                             "a trampoline only forwards to its sink or runs the engine function, so its body may call nothing but " +
                             "a method of a sink interface or the engine function's pointer.");
            }

            if (!hook.Operations.Any(operation => operation.IsSinkCall))
                problems.Add($"{name} calls no sink: a trampoline forwards to the sink of its hook file.");
            if (!hook.Operations.Any(operation => operation.IsFunctionPointerCall))
                problems.Add($"{name} never runs the engine function: with no sink bound a trampoline calls the engine function itself.");
            if (hook.VolatileStaticReads.Count == 0)
                problems.Add($"{name} reads no volatile field: a trampoline reads its bound sink from a volatile static field.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }

    /// <param name="facts">The hooks of the assembly.</param>
    /// <param name="allowedNamespace">The one namespace that may hold <c>[UnmanagedCallersOnly]</c> methods, which are
    /// then not hooks of an engine function (the native entry's NWNX callbacks).</param>
    public static IReadOnlyList<string> EntryPointsOutside(NativeHookFacts facts, string allowedNamespace)
    {
        return facts.Hooks
            .Where(hook => hook.Owner.Namespace != allowedNamespace)
            .Select(hook => $"{hook.Owner}::{hook.Method} is an [UnmanagedCallersOnly] entry point of the game: a native hook's " +
                            "trampoline lives in SWLOR.Game.NativeHooks, which outlives every game build, and forwards to a sink.")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
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
    public static IReadOnlyList<string> ContractsReferenceViolations(IEnumerable<string> referencedAssemblies, IReadOnlyCollection<string> allowed) =>
        ReferenceViolations("The contracts assembly", referencedAssemblies, allowed);

    /// <param name="referencedAssemblies">The simple names of the assemblies the hooks assembly references.</param>
    /// <param name="allowed">The names it may reference besides the framework (<c>System.*</c>, <c>netstandard</c>, <c>mscorlib</c>).</param>
    public static IReadOnlyList<string> HooksReferenceViolations(IEnumerable<string> referencedAssemblies, IReadOnlyCollection<string> allowed) =>
        ReferenceViolations("The native hooks assembly", referencedAssemblies, allowed);

    private static IReadOnlyList<string> ReferenceViolations(string assembly, IEnumerable<string> referencedAssemblies, IReadOnlyCollection<string> allowed)
    {
        return referencedAssemblies
            .Where(name => !IsFramework(name) && !allowed.Contains(name))
            .Select(name => $"{assembly} references {name}: it may reference only the framework and {string.Join(", ", allowed.OrderBy(a => a, StringComparer.Ordinal))}, never anything of the game.")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsFramework(string name) =>
        name == "System" || name.StartsWith("System.", StringComparison.Ordinal) || name == "netstandard" || name == "mscorlib";
}

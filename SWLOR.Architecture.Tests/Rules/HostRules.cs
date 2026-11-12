namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The rule of the permanent host (P4-10): <c>SWLOR.Game.Host</c> outlives every game build, so it references no game
/// type. It may reference only the framework, the engine bindings, the hosting package and its contracts, the SWLOR
/// contracts and Redis; it reaches the game only through those contracts.
/// </summary>
internal static class HostRules
{
    /// <param name="referencedAssemblies">The simple names of the assemblies the host assembly references.</param>
    /// <param name="allowed">The names it may reference besides the framework (<c>System.*</c>, <c>netstandard</c>, <c>mscorlib</c>).</param>
    public static IReadOnlyList<string> HostReferenceViolations(IEnumerable<string> referencedAssemblies, IReadOnlyCollection<string> allowed)
    {
        return referencedAssemblies
            .Where(name => !IsFramework(name) && !allowed.Contains(name))
            .Select(name => $"The host assembly references {name}: it outlives every game build, so it may reference only the framework and " +
                            $"{string.Join(", ", allowed.OrderBy(a => a, StringComparer.Ordinal))}, never anything of the game.")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsFramework(string name) =>
        name == "System" || name.StartsWith("System.", StringComparison.Ordinal) || name == "netstandard" || name == "mscorlib";
}

using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// Discovery scans see only the game build's own assemblies (P4-4): the one helper (<c>Core.GameAssemblies</c>) may list the
/// assemblies of the process, and nothing else may enumerate them, so a retired build that is still loaded during a reload is
/// never searched for types. Covers <c>AppDomain.GetAssemblies</c> and the listings of <c>AssemblyLoadContext</c>
/// (<c>All</c>, and <c>Assemblies</c> on any context, <c>Default</c> included).
/// </summary>
internal static class AssemblyScanRule
{
    private static readonly (string Type, string Member)[] Forbidden =
    [
        ("System.AppDomain", "GetAssemblies"),
        ("System.Runtime.Loader.AssemblyLoadContext", "get_All"),
        ("System.Runtime.Loader.AssemblyLoadContext", "get_Assemblies")
    ];

    /// <param name="facts">The uses of members of other assemblies' types.</param>
    /// <param name="helper">The one type allowed to enumerate assemblies.</param>
    public static IReadOnlyList<string> Find(AssemblyFacts facts, TypeKey helper)
    {
        var problems = new List<string>();

        foreach (var use in facts.ExternalMemberUses)
        {
            if (use.Source == helper || !Forbidden.Contains((use.ExternalType, use.MemberName)))
                continue;

            problems.Add($"{use.Source} uses {use.ExternalType}.{use.MemberName} ({string.Join(", ", use.Members.OrderBy(member => member, StringComparer.Ordinal))}): only {helper} may list assemblies. Scan GameAssemblies.GetAssemblies() instead.");
        }

        return problems.OrderBy(message => message, StringComparer.Ordinal).ToList();
    }
}

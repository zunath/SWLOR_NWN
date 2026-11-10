using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Rules;

/// <summary>
/// The namespace guard of the maintainability refactor (P2-Z2, owner decision 2026-11-06: one root per domain and no
/// namespace that hides a type). It reads the namespaces and top-level type names of one compiled assembly and reports,
/// with no exceptions:
/// <list type="bullet">
/// <item>a namespace segment equal to the name of a type of the assembly (inside that namespace the simple name means the
/// namespace, so the type is hidden: <c>Domain.Combat</c> hid the facade <c>Combat</c>);</item>
/// <item>a leaf segment under two parents (a domain named <c>Core</c> beside the root namespace <c>Core</c>: inside
/// <c>Domain.*</c> the name <c>Core</c> means the wrong one);</item>
/// <item>any <c>Service.&lt;Name&gt;Service</c> namespace (the support namespaces P2-Z2 moved into <c>Domain.&lt;Domain&gt;</c>).</item>
/// </list>
/// Every namespace counts, including the ones that hold no type of their own (<c>SWLOR.Game.Server.Domain</c>).
/// </summary>
internal static class NamespaceRules
{
    private const string ServiceSegment = "Service";

    /// <param name="types">The top-level types of the assembly (compiler-generated names, which start with <c>&lt;</c>, are ignored).</param>
    public static IReadOnlyList<string> Violations(IEnumerable<TypeKey> types)
    {
        var declared = types.Where(type => !type.Name.StartsWith('<')).ToList();
        var typeNames = declared
            .GroupBy(type => type.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(type => type.ToString()).OrderBy(name => name, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
        var namespaces = AllNamespaces(declared.Select(type => type.Namespace));

        var problems = new List<string>();
        foreach (var @namespace in namespaces)
        {
            foreach (var segment in @namespace.Split('.').Distinct(StringComparer.Ordinal))
            {
                if (typeNames.TryGetValue(segment, out var clashing))
                    problems.Add($"Namespace {@namespace}: the segment '{segment}' is also a type ({string.Join(", ", clashing.Take(3))}), which the namespace hides.");
            }
        }

        foreach (var leaf in namespaces
                     .Where(@namespace => @namespace.Contains('.'))
                     .GroupBy(@namespace => @namespace[(@namespace.LastIndexOf('.') + 1)..], StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            problems.Add($"The namespace segment '{leaf.Key}' is the leaf of {leaf.Count()} namespaces ({string.Join(", ", leaf)}): give each a name of its own.");
        }

        foreach (var @namespace in namespaces)
        {
            var segments = @namespace.Split('.');
            for (var i = 0; i + 1 < segments.Length; i++)
            {
                if (segments[i] == ServiceSegment && segments[i + 1].EndsWith(ServiceSegment, StringComparison.Ordinal))
                {
                    problems.Add($"Namespace {@namespace} is a Service.<Name>Service support namespace: put the types in Domain.<Domain>.");
                    break;
                }
            }
        }

        return problems;
    }

    /// <summary>Every namespace the given ones declare, the enclosing ones included, sorted ordinally.</summary>
    private static List<string> AllNamespaces(IEnumerable<string> namespaces)
    {
        var all = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var @namespace in namespaces.Where(name => name.Length > 0))
        {
            var current = @namespace;
            while (all.Add(current))
            {
                var dot = current.LastIndexOf('.');
                if (dot < 0)
                    break;

                current = current[..dot];
            }
        }

        return all.ToList();
    }
}

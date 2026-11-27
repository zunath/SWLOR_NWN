using System.Text;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>The edge totals per pair of domains, printed in the test output.</summary>
internal static class DomainPairReport
{
    public static string Render(string title, IReadOnlyCollection<UnitEdge> edges)
    {
        var text = new StringBuilder();
        text.AppendLine($"{title}: {edges.Count} unit edge(s), {edges.Select(e => e.DomainPair).Distinct().Count()} domain pair(s)");

        var pairs = edges
            .GroupBy(edge => edge.DomainPair)
            .Select(group => (Pair: group.Key, Edges: group.Count(), Members: group.Sum(edge => edge.MemberCount)))
            .OrderByDescending(pair => pair.Edges)
            .ThenBy(pair => pair.Pair, StringComparer.Ordinal);
        foreach (var pair in pairs)
            text.AppendLine($"  {pair.Pair,-34} {pair.Edges,5} edge(s) {pair.Members,7} member(s)");

        return text.ToString();
    }
}

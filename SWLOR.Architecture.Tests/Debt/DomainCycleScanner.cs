using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Pairs of domains that depend on each other (Phase 5 target 4), from the cross-domain edges of the compiled server
/// (<see cref="DomainDependencies.Edges"/>): a domain is its <c>Domain/</c> code plus its content folders as
/// <c>DomainMap.json</c> defines them, and <c>Foundation</c> and <c>Platform</c> are domains like the others here.
/// </summary>
internal static class DomainCycleScanner
{
    public static IReadOnlyList<DomainCycle> Scan(IEnumerable<UnitEdge> edges)
    {
        var directions = edges
            .GroupBy(edge => (edge.FromDomain, edge.ToDomain))
            .ToDictionary(group => group.Key, group => group.Count());

        return directions.Keys
            .Where(pair => string.CompareOrdinal(pair.FromDomain, pair.ToDomain) < 0 && directions.ContainsKey((pair.ToDomain, pair.FromDomain)))
            .Select(pair => new DomainCycle(pair.FromDomain, pair.ToDomain, directions[pair], directions[(pair.ToDomain, pair.FromDomain)]))
            .OrderBy(cycle => cycle.Key, StringComparer.Ordinal)
            .ToList();
    }
}

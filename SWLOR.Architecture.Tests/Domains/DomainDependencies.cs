using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// A type reference graph lifted to units and domains: which unit depends on which unit of another domain.
/// </summary>
internal sealed class DomainDependencies
{
    private DomainDependencies(
        DomainMap map,
        IReadOnlyList<DependencyUnit> units,
        IReadOnlyList<DependencyUnit> unmappedUnits,
        IReadOnlyList<UnitEdge> edges)
    {
        Map = map;
        Units = units;
        UnmappedUnits = unmappedUnits;
        Edges = edges;
    }

    public DomainMap Map { get; }

    /// <summary>Every unit that has at least one type in the assembly, sorted by name.</summary>
    public IReadOnlyList<DependencyUnit> Units { get; }

    /// <summary>Services and Feature folders that are in no domain of the map.</summary>
    public IReadOnlyList<DependencyUnit> UnmappedUnits { get; }

    /// <summary>
    /// Every edge between units of different domains, sorted by name. An edge from or to an unmapped unit is left
    /// out; the unmapped unit is reported on its own.
    /// </summary>
    public IReadOnlyList<UnitEdge> Edges { get; }

    /// <summary>The edges the guard compares with the baseline: all cross-domain edges except those into Core or Platform.</summary>
    public IEnumerable<UnitEdge> EnforcedEdges => Edges.Where(edge => !Map.IsUnrestrictedTarget(edge.ToDomain));

    /// <summary>The edges into Core or Platform: recorded for information, never failing.</summary>
    public IEnumerable<UnitEdge> UnrestrictedEdges => Edges.Where(edge => Map.IsUnrestrictedTarget(edge.ToDomain));

    public static DomainDependencies Build(TypeReferenceGraph graph, DomainMap map)
    {
        var unitOfType = new Dictionary<TypeKey, DependencyUnit?>();
        DependencyUnit? UnitOf(TypeKey type)
        {
            if (!unitOfType.TryGetValue(type, out var unit))
            {
                unit = UnitClassifier.Classify(type, map);
                unitOfType[type] = unit;
            }

            return unit;
        }

        var units = graph.Types
            .Select(UnitOf)
            .OfType<DependencyUnit>()
            .Distinct()
            .OrderBy(unit => unit.ToString(), StringComparer.Ordinal)
            .ToList();
        var unmapped = units.Where(unit => map.DomainOf(unit) is null).ToList();

        var members = new Dictionary<(DependencyUnit From, DependencyUnit To), HashSet<string>>();
        foreach (var ((source, target), referencing) in graph.References)
        {
            if (UnitOf(source) is not { } from || UnitOf(target) is not { } to || from == to)
                continue;
            if (map.DomainOf(from) is null || map.DomainOf(to) is null)
                continue;
            if (map.IsWiringSource(from))
                continue;

            if (!members.TryGetValue((from, to), out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                members[(from, to)] = set;
            }

            set.UnionWith(referencing);
        }

        var edges = new List<UnitEdge>();
        foreach (var ((from, to), set) in members)
        {
            var fromDomain = map.DomainOf(from)!;
            var toDomain = map.DomainOf(to)!;
            if (fromDomain != toDomain)
                edges.Add(new UnitEdge(from, to, fromDomain, toDomain, set));
        }

        edges.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        return new DomainDependencies(map, units, unmapped, edges);
    }
}

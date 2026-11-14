using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The domain order measures (P5-1b, Phase 5 target 4) over the server's source references
/// (<see cref="SourceReferenceScanner"/>), counted the way <c>MaintainabilityRefactorDomainOrder.md</c> counts them: each domain
/// is two nodes, its logic and its content (<see cref="OrderNode"/>), <c>platformAreaDomains</c> puts <c>Entity</c> and
/// <c>Enumeration</c> in Foundation, and references out of a wiring area (the composition root, the native sinks) do not count.
/// Only references that stay in their band are measured: content using logic is allowed, and logic naming content is the
/// no-content rule (<c>LogicToContent.txt</c>).
/// </summary>
internal static class DomainCycleScanner
{
    /// <summary>Pairs of nodes of one band that reference each other, with their unit edges and members each way.</summary>
    public static IReadOnlyList<DomainCycle> Scan(IEnumerable<SourceReference> references, DomainMap map)
    {
        var directions = new Dictionary<(OrderNode From, OrderNode To), (HashSet<(DependencyUnit, DependencyUnit)> Edges, HashSet<string> Members)>();
        foreach (var (reference, from, to, _) in InBand(references, map))
        {
            if (!directions.TryGetValue((from, to), out var direction))
            {
                direction = (new HashSet<(DependencyUnit, DependencyUnit)>(), new HashSet<string>(StringComparer.Ordinal));
                directions[(from, to)] = direction;
            }

            direction.Edges.Add((reference.From, reference.To));
            direction.Members.Add(reference.Member);
        }

        return directions.Keys
            .Where(pair => string.CompareOrdinal(pair.From.ToString(), pair.To.ToString()) < 0 && directions.ContainsKey((pair.To, pair.From)))
            .Select(pair =>
            {
                var forward = directions[pair];
                var backward = directions[(pair.To, pair.From)];
                return new DomainCycle(pair.From.ToString(), pair.To.ToString(), forward.Edges.Count, backward.Edges.Count, forward.Members.Count, backward.Members.Count);
            })
            .OrderBy(cycle => cycle.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The members of each file that reference a node higher in the order, in their own band.</summary>
    public static IReadOnlyList<UpwardReference> Upward(IEnumerable<SourceReference> references, DomainMap map)
    {
        return InBand(references, map)
            .Where(found => found.Direction == OrderDirection.Up)
            .GroupBy(found => (found.Reference.File, found.From, found.To))
            .Select(group => new UpwardReference(
                group.Key.File,
                group.Key.From,
                group.Key.To,
                group.Select(found => found.Reference.Member).ToHashSet(StringComparer.Ordinal),
                group.Select(found => found.Reference.Target).Distinct().OrderBy(target => target, StringComparer.Ordinal).ToList(),
                group.Select(found => (found.Reference.From, found.Reference.To)).ToHashSet()))
            .OrderBy(upward => upward.Key, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The references between two different nodes of one band, with their direction (up or down).</summary>
    private static IEnumerable<(SourceReference Reference, OrderNode From, OrderNode To, OrderDirection Direction)> InBand(IEnumerable<SourceReference> references, DomainMap map)
    {
        foreach (var reference in references)
        {
            if (map.IsWiringSource(reference.From) || map.NodeOf(reference.From) is not { } from || map.NodeOf(reference.To) is not { } to)
                continue;

            var direction = map.DirectionOf(from, to);
            if (direction is OrderDirection.Up or OrderDirection.Down)
                yield return (reference, from, to, direction);
        }
    }
}

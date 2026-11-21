using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Static entry points and static state of the server (target 1), read from the compiled IL: every type with a
/// static field that can change or share what it holds (anything but a <c>const</c> or a <c>static readonly</c> field of a
/// deeply immutable type, the immutability rule of <see cref="ImmutabilityAnalyzer"/>), and every type outside the lookup itself
/// whose static members resolve a service from the container (<c>NativeHookAccess</c>). The composition root
/// (<c>Composition</c>) is not measured for state: holding the container is its job. The lookup itself
/// (<c>GameServices</c>, <c>FacadeAccessor</c>, <c>FacadeCache</c>) is measured by the lookup measure.
/// </summary>
internal static class StaticStateScanner
{
    public const string CompositionNamespace = UnitClassifier.RootNamespace + ".Composition";

    private static readonly string[] LookupTypes = ["GameServices", "FacadeAccessor", "FacadeCache"];

    public static IReadOnlyList<StaticStateHolder> Scan(AssemblyFacts facts, TypeReferenceGraph graph)
    {
        var accessor = new TypeKey(CompositionNamespace, "FacadeAccessor");

        var fields = facts.StaticFields
            .Where(field => !field.IsLiteral && !(field.IsInitOnly && field.IsDeeplyImmutable))
            .Where(field => !InComposition(field.Owner))
            .GroupBy(field => field.Owner)
            .ToDictionary(group => group.Key, group => group.OrderBy(field => field.Declaring, StringComparer.Ordinal).ThenBy(field => field.Name, StringComparer.Ordinal).ToList());

        var accessors = graph.References
            .Where(pair => pair.Key.Target == accessor && !IsLookup(pair.Key.Source))
            .ToDictionary(pair => pair.Key.Source, pair => pair.Value.OrderBy(member => member, StringComparer.Ordinal).ToList());

        return fields.Keys.Union(accessors.Keys)
            .Where(type => UnitClassifier.Classify(type) != null)
            .Select(type => new StaticStateHolder(
                type,
                fields.TryGetValue(type, out var held) ? held : [],
                accessors.TryGetValue(type, out var members) ? members : [],
                UnitClassifier.Classify(type) is { Kind: UnitKind.FeatureFolder or UnitKind.FeatureType }))
            .OrderBy(holder => holder.Type.ToString(), StringComparer.Ordinal)
            .ToList();
    }

    private static bool InComposition(TypeKey type) =>
        type.Namespace == CompositionNamespace || type.Namespace.StartsWith(CompositionNamespace + ".", StringComparison.Ordinal);

    private static bool IsLookup(TypeKey type) => type.Namespace == CompositionNamespace && LookupTypes.Contains(type.Name);
}

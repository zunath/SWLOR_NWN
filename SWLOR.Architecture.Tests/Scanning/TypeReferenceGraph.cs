namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Which top-level types of an assembly mention which other top-level types of the same assembly, read from the
/// compiled IL. Each edge records the members that hold the reference, as informational detail.
/// </summary>
internal sealed class TypeReferenceGraph
{
    public TypeReferenceGraph(
        IReadOnlyList<TypeKey> types,
        IReadOnlyDictionary<(TypeKey Source, TypeKey Target), IReadOnlySet<string>> references)
    {
        Types = types;
        References = references;
    }

    /// <summary>Every top-level type of the assembly, compiler-generated ones included.</summary>
    public IReadOnlyList<TypeKey> Types { get; }

    /// <summary>
    /// Source type, target type and the distinct members of the source (including its closures and lambdas) that
    /// hold a reference to the target. A type never references itself in this map.
    /// </summary>
    public IReadOnlyDictionary<(TypeKey Source, TypeKey Target), IReadOnlySet<string>> References { get; }
}

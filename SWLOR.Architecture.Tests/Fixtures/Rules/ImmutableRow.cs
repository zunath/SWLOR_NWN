namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A sealed record with init-only members of immutable types: accepted as a table element.</summary>
public sealed record ImmutableRow(string Name, int Level)
{
    public ImmutableTableKind Kind { get; init; }

    public System.Collections.Immutable.ImmutableArray<int> Steps { get; init; }
}

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A record that refers back to itself: the cycle guard must not loop and the type is still immutable.</summary>
public sealed record ImmutableNode(string Name)
{
    public ImmutableNode? Parent { get; init; }
}

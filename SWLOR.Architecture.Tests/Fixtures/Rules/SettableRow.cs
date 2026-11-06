namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A record with a settable member: rejected.</summary>
public sealed record SettableRow(string Name)
{
    public int Level { get; set; }
}

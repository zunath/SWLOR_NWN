namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A class that is not a record, even though its member is get-only: its immutability cannot be proven.</summary>
public sealed class PlainClassRow
{
    public string Name { get; } = string.Empty;
}

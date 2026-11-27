namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A read-only record struct: accepted as a table element.</summary>
public readonly record struct ImmutableRowStruct(int Low, int High);

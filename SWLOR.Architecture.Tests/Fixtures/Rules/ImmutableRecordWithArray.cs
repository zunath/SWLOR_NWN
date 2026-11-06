namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>A record whose init-only member holds an array: rejected.</summary>
public sealed record ImmutableRecordWithArray(int[] Values);

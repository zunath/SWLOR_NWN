namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>A type that implements a sink interface.</summary>
/// <param name="Type">The implementing top-level type.</param>
/// <param name="Interface">The simple name of the sink interface.</param>
internal sealed record SinkImplementer(TypeKey Type, string Interface);

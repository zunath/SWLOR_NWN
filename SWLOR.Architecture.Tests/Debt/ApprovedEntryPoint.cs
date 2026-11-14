namespace SWLOR.Architecture.Tests.Debt;

/// <summary>A static entry point the owner approved to stay static (Phase 5 owner decision 3).</summary>
/// <param name="Type">The full type name.</param>
/// <param name="Reason">Why it stays.</param>
internal sealed record ApprovedEntryPoint(string Type, string Reason);

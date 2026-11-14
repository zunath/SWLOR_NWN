namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One line of a debt baseline: what carries the debt and how much of it.</summary>
/// <param name="Key">What carries it: a file, a type, a method, a pair of domains.</param>
/// <param name="Count">How much: lines, references, services. The guard fails when it changes in either direction.</param>
/// <param name="Detail">Information for the report and failure messages only; never written to the baseline or compared.</param>
internal sealed record DebtEntry(string Key, int Count, string Detail = "");

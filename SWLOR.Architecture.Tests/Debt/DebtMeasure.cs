namespace SWLOR.Architecture.Tests.Debt;

/// <summary>One measured kind of Phase 5 debt and its entries as they are in the code right now.</summary>
/// <param name="Title">The heading the report and failure messages use.</param>
/// <param name="FileName">The baseline file under <c>Baseline/Phase5</c>.</param>
/// <param name="Header">The comment lines at the top of the baseline file: what is measured, how, and the unit of the count.</param>
/// <param name="Entries">Every entry, in any order (the baseline file sorts them).</param>
/// <param name="Summary">The totals the report shows for this measure, one line each.</param>
internal sealed record DebtMeasure(
    string Title,
    string FileName,
    IReadOnlyList<string> Header,
    IReadOnlyList<DebtEntry> Entries,
    IReadOnlyList<string> Summary);

namespace SWLOR.ContentInjection;

/// <summary>One run of the tool.</summary>
/// <param name="Mode">Apply, check or reverse.</param>
/// <param name="Root">A repository root for <see cref="RunMode.Apply"/> and <see cref="RunMode.Check"/>, any folder for <see cref="RunMode.Reverse"/>.</param>
/// <param name="DataPath"><c>tools/Refactor/ContentInjection.json</c>, the list of converted content files (the branch's own).</param>
internal sealed record RunOptions(RunMode Mode, string Root, string DataPath);

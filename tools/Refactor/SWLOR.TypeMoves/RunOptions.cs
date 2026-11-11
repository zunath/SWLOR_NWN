namespace SWLOR.TypeMoves;

/// <summary>One run of the tool.</summary>
/// <param name="Mode">Apply or check.</param>
/// <param name="Root">The repository root to rewrite (the branch, or the parity copy of master).</param>
/// <param name="DataPath">
/// <c>TypeMoves.json</c>. The wrapper passes the branch's own file, so a run against another tree (the parity copy of master)
/// moves the branch's list.
/// </param>
internal sealed record RunOptions(RunMode Mode, string Root, string DataPath);

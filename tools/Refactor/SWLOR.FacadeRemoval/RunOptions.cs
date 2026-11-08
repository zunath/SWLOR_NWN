namespace SWLOR.FacadeRemoval;

/// <summary>One run of the tool.</summary>
/// <param name="Mode">Apply, check or reverse.</param>
/// <param name="Root">
/// The tree to rewrite: a repository root for <see cref="RunMode.Apply"/> and <see cref="RunMode.Check"/>, any folder for
/// <see cref="RunMode.Reverse"/>.
/// </param>
/// <param name="DataPath">
/// <c>RemovedFacades.json</c>. <c>HandlerRenames.txt</c> is read from the same folder. The wrapper passes the branch's own
/// files, so a run against another tree (the parity copy of master) uses the branch's data.
/// </param>
internal sealed record RunOptions(RunMode Mode, string Root, string DataPath)
{
    public string RenamesPath => Path.Combine(Path.GetDirectoryName(DataPath) ?? string.Empty, "HandlerRenames.txt");
}

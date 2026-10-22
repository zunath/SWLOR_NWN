namespace SWLOR.CraftSim.Gates
{
    /// <summary>A titled table of evidence a gate attaches to its verdict.</summary>
    public sealed record GateTable(string Title, IReadOnlyList<string> Header, IReadOnlyList<IReadOnlyList<string>> Rows);
}

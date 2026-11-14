namespace SWLOR.Architecture.Tests.Debt;

/// <summary>Two domains that depend on each other.</summary>
/// <param name="First">The domain first in ordinal order.</param>
/// <param name="Second">The other domain.</param>
/// <param name="ForwardEdges">Unit edges from <paramref name="First"/> to <paramref name="Second"/>.</param>
/// <param name="BackwardEdges">Unit edges from <paramref name="Second"/> to <paramref name="First"/>.</param>
internal sealed record DomainCycle(string First, string Second, int ForwardEdges, int BackwardEdges)
{
    public string Key => $"{First} <-> {Second}";

    /// <summary>
    /// The unit edges each way, for the report. Referencing members are not counted: the optimizer removes some in a Release
    /// build, so they differ between build configurations (CrossDomainEdges.txt lists them per edge, for information).
    /// </summary>
    public string Detail => $"{First} -> {Second}: {ForwardEdges} edge(s); {Second} -> {First}: {BackwardEdges} edge(s)";
}

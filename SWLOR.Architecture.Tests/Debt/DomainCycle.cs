namespace SWLOR.Architecture.Tests.Debt;

/// <summary>Two nodes of one band of the domain order that depend on each other.</summary>
/// <param name="First">The node first in ordinal order (<c>Abilities</c>, <c>Abilities content</c>).</param>
/// <param name="Second">The other node.</param>
/// <param name="ForwardEdges">Unit edges from <paramref name="First"/> to <paramref name="Second"/>.</param>
/// <param name="BackwardEdges">Unit edges from <paramref name="Second"/> to <paramref name="First"/>.</param>
/// <param name="ForwardMembers">Source members that reference <paramref name="Second"/> from <paramref name="First"/>.</param>
/// <param name="BackwardMembers">Source members that reference <paramref name="First"/> from <paramref name="Second"/>.</param>
internal sealed record DomainCycle(string First, string Second, int ForwardEdges, int BackwardEdges, int ForwardMembers = 0, int BackwardMembers = 0)
{
    public string Key => $"{First} <-> {Second}";

    /// <summary>
    /// The unit edges and source members each way, for the report. The members are read from source, so they do not depend
    /// on the build configuration; the baseline compares the unit edges, which change less often.
    /// </summary>
    public string Detail =>
        $"{First} -> {Second}: {ForwardEdges} edge(s), {ForwardMembers} member(s); {Second} -> {First}: {BackwardEdges} edge(s), {BackwardMembers} member(s)";
}

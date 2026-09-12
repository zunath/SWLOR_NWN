namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration;

public enum DecorationPlacementStyle
{
    Spacious,
    Compact
}

public sealed record DecorationPlacementReport(
    int ProposedCount,
    int PlacedCount,
    int UnsupportedCount,
    int RouteConflictCount,
    int OverlapCount)
{
    public int OmittedCount => ProposedCount - PlacedCount;
}

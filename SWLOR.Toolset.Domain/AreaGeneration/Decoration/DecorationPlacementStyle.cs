namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration;

public enum DecorationPlacementStyle
{
    Spacious,
    Compact
}

/// <summary>Summarizes proposed and accepted decoration counts and mutually exclusive reasons for omitting props.</summary>
public sealed record DecorationPlacementReport(
    int ProposedCount,
    int PlacedCount,
    int UnsupportedCount,
    int RouteConflictCount,
    int OverlapCount)
{
    public int OmittedCount => ProposedCount - PlacedCount;
}

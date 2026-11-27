namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>Where a performer's venue rating comes from: the area, and a Dance Floor within reach.</summary>
    public sealed record PerformanceVenueBreakdown(int Total, int AreaRating, string AreaText, bool DanceFloor);
}

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>One performer in range, as a row of the Watching window.</summary>
    public sealed record PerformanceWatchRow(uint Performer, string Name, string Detail, bool IsFollowed);
}

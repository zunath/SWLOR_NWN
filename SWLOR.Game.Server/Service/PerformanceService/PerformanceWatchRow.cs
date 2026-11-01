namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// One performer in range, as a row of the Watching window. IsChosen marks the performer the watcher picked, as opposed
    /// to the strongest performer followed automatically.
    /// </summary>
    public sealed record PerformanceWatchRow(uint Performer, string Name, string Detail, bool IsFollowed, bool IsChosen = false);
}

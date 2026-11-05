namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>A Cantina song a musician can play: its ambientmusic.2da track and its name.</summary>
    public sealed record PerformanceSongOption(int TrackId, string Name);
}

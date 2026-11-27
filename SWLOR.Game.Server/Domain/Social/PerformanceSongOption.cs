namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>A Cantina song a musician can play: its ambientmusic.2da track and its name.</summary>
    public sealed record PerformanceSongOption(int TrackId, string Name);
}

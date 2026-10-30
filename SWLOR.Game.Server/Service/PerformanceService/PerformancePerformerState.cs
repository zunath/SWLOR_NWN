namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>What the audience tick remembers about one performing session between ticks.</summary>
    public sealed class PerformancePerformerState
    {
        public PerformancePerformerState(DateTime startedUtc, int songTrackId)
        {
            LastPaidUtc = startedUtc;
            SongTrackId = songTrackId;
        }

        /// <summary>When Performance XP was last paid; a first tick shorter than a minute pays only the time performed.</summary>
        public DateTime LastPaidUtc { get; set; }

        /// <summary>The musician's song: chosen when the session first appears, and changed only when the musician picks another in the Perform window.</summary>
        public int SongTrackId { get; set; }
    }
}

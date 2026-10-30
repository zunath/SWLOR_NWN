namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>What the audience tick tells a performer's cue window: who is watching and the venue. Sent once a minute, never per beat.</summary>
    public sealed record PerformerAudienceInfo
    {
        public string VenueText { get; init; } = string.Empty;
        public string FollowingText { get; init; } = string.Empty;
        public string InspiringText { get; init; } = string.Empty;

        /// <summary>The band line of the Session window; empty for dancers.</summary>
        public string BandText { get; init; } = string.Empty;

        /// <summary>The song the audience hears, which is the band leader's; empty for dancers.</summary>
        public string AudienceHearsText { get; init; } = string.Empty;
    }
}

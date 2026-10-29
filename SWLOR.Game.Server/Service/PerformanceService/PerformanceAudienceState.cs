namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// What the audience tick remembers about one player between ticks: their warm-up, whom they follow, and the
    /// Inspired time their last tick set. Holds no engine calls.
    /// </summary>
    public sealed class PerformanceAudienceState
    {
        public PerformanceAudienceState(uint creature)
        {
            Creature = creature;
        }

        public uint Creature { get; }

        /// <summary>The performers the player picked in the Watching window; 0 means follow the strongest.</summary>
        public uint DancerChoice { get; set; }
        public uint MusicianChoice { get; set; }

        /// <summary>The performers followed at the last tick. They break ties, and tell a Flourish who is listening.</summary>
        public uint FollowedDancer { get; set; }
        public uint FollowedMusician { get; set; }

        public int ContinuousMinutes { get; set; }
        public int LastSeenTick { get; set; }

        /// <summary>The band song the player hears instead of their normal music; null when they hear their own.</summary>
        public int? BandTrackId { get; set; }

        /// <summary>The area the band song was sent in; a client forgets it on leaving the area.</summary>
        public uint BandArea { get; set; }

        /// <summary>The remaining Inspired seconds the last tick set. Valid while HasFrozenRemaining.</summary>
        public bool HasFrozenRemaining { get; set; }
        public double FrozenRemainingSeconds { get; set; }
        public int CapMinutes { get; set; }

        public DateTime? LastFlourishBankUtc { get; set; }
        public double LastFlourishBankedMinutes { get; set; }

        public bool IsWarmedUp => ContinuousMinutes >= PerformanceMath.WarmupMinutes;
        public bool HasChoice => DancerChoice != 0 || MusicianChoice != 0;

        public bool Follows(uint performer)
        {
            return performer != 0 && (FollowedDancer == performer || FollowedMusician == performer);
        }

        /// <summary>The player left every performer's range: the warm-up starts over and Inspired counts down again.</summary>
        public void StopWatching()
        {
            ContinuousMinutes = 0;
            FollowedDancer = 0;
            FollowedMusician = 0;
            HasFrozenRemaining = false;
        }
    }
}

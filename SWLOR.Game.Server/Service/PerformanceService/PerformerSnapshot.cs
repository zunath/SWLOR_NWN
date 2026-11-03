using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// One performer as the audience tick sees them. Their stats, unlocks and venue are read once per tick,
    /// so every watcher in range reuses the same snapshot and the pure audience rules never touch the engine.
    /// </summary>
    public sealed class PerformerSnapshot
    {
        public uint Performer { get; init; }
        public PerformanceKind Kind { get; init; }
        public DateTime StartedUtc { get; init; }
        public PerformanceQuality Quality { get; init; } = PerformanceQuality.Steady;
        public PerformanceTempo Tempo { get; init; } = PerformanceTempo.Relaxed;
        public Vector3 Position { get; init; }
        public float RangeMeters { get; init; } = PerformanceAudienceRules.BaseRangeMeters;
        public int Venue { get; init; }

        /// <summary>The rank value of the performer's XP bonus (6, 12, 18 or 24) before SOC scaling.</summary>
        public int XpBase { get; init; }

        /// <summary>The XP bonus the performer's Inspired grants after SOC scaling; what the default follow choice ranks by.</summary>
        public int XpScaled { get; init; }

        public int DebtClearPercent { get; init; }

        /// <summary>Crowd Pleaser for a dancer, Harmony for a musician: the percent faster the audience banks.</summary>
        public int BankingBonusPercent { get; init; }

        public int CapBonusMinutes { get; init; }
        public int LightFeetSpeedPercent { get; init; }
        public int RoutineAttributeBonus { get; init; }
        public int SteadyHandsBonus { get; init; }
        public int AnglersLullabyBonus { get; init; }
        public int SetAttributeBonus { get; init; }

        /// <summary>Showstopper for a dancer, Virtuoso for a musician.</summary>
        public bool HasCapstone { get; init; }

        /// <summary>Partner Dance for a dancer, Duet for a musician.</summary>
        public bool HasPartnerTrait { get; init; }

        public bool HasStandingOvation { get; init; }
        public bool HasRequiem { get; init; }
        public PerformanceRoutineType Routine { get; init; }
        public PerformanceRoutineType SecondRoutine { get; init; }
        public PerformanceSetType Set { get; init; }
        public PerformanceSetType SecondSet { get; init; }

        /// <summary>The ambientmusic.2da track this musician plays; the band leader's track is what the band's audience hears.</summary>
        public int SongTrackId { get; init; }

        /// <summary>The live session this snapshot describes. Null in tests.</summary>
        public PerformanceSession Session { get; init; }

        // Worked out for the whole show by PerformanceShow.Prepare.
        public PerformerSnapshot Partner { get; set; }

        /// <summary>The musician whose song the band plays: whoever in the band started first. A musician alone leads their own band.</summary>
        public PerformerSnapshot BandLeader { get; set; }
        public bool HarmonyActive { get; set; }
        public int? BandTrackId { get; set; }

        // Counted while the tick visits watchers.
        public int WatcherCount { get; set; }
        public int Crowd { get; set; }
    }
}

using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// What the audience tick remembers about one player between ticks: their warm-up, whom they follow, and the
    /// Inspired time their last tick set. Holds no engine calls.
    /// </summary>
    public sealed class PerformanceAudienceState
    {
        /// <summary>How many ticks a performer's name is reused before the name lookup runs again.</summary>
        public const int NameLifetimeTicks = 5;

        private readonly Dictionary<uint, string> _names = new();
        private int _namesTick;

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

        /// <summary>When the first tick of this stretch of watching saw the player; minutes are counted from it.</summary>
        public DateTime? WatchingSinceUtc { get; set; }
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

        /// <summary>The player closed the Watching window; it stays closed until they start watching a new show.</summary>
        public bool WatchingDismissed { get; set; }

        /// <summary>The area of the show the Watching window last reported, so a window closed by changing area is not a dismissal.</summary>
        public uint WatchingArea { get; set; }

        /// <summary>The last tick's entry and report, kept so a Follow click can redraw the window without waiting for the next tick.</summary>
        public PerformanceWatcherEntry LastEntry { get; set; }
        public PerformanceWatcherReport LastReport { get; set; }

        public bool IsWarmedUp => ContinuousMinutes >= PerformanceMath.WarmupMinutes;

        /// <summary>True while the player has watching progress or a Watching window that <see cref="StopWatching"/> would clear.</summary>
        public bool IsWatching => WatchingSinceUtc.HasValue || ContinuousMinutes > 0 || WatchingArea != 0 || LastEntry != null ||
                                  BandTrackId.HasValue;
        public bool HasChoice => DancerChoice != 0 || MusicianChoice != 0;

        public bool Follows(uint performer)
        {
            return performer != 0 && (FollowedDancer == performer || FollowedMusician == performer);
        }

        /// <summary>
        /// The name this watcher knows a performer by. Looked up when first needed and then reused for a few ticks, so a
        /// tick does not run a name lookup per performer per watcher every minute.
        /// </summary>
        public string GetName(uint performer, int tick, Func<string> lookup)
        {
            if (tick - _namesTick >= NameLifetimeTicks)
            {
                _names.Clear();
                _namesTick = tick;
            }

            if (!_names.TryGetValue(performer, out var name))
            {
                name = lookup();
                _names[performer] = name;
            }

            return name;
        }

        /// <summary>The player left every performer's range: the warm-up starts over and Inspired counts down again.</summary>
        public void StopWatching()
        {
            WatchingSinceUtc = null;
            ContinuousMinutes = 0;
            FollowedDancer = 0;
            FollowedMusician = 0;
            HasFrozenRemaining = false;
            WatchingDismissed = false;
            WatchingArea = 0;
            LastEntry = null;
            LastReport = null;
            _names.Clear();
        }
    }
}

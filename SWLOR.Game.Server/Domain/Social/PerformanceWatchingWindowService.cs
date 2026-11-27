using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a watcher does in the Watching window: the logic that was <see cref="PerformanceAudienceService"/>'s own, moved
    /// here unchanged. The watcher states it reads are <see cref="IPerformanceWatchingService"/>'s.
    /// </summary>
    public class PerformanceWatchingWindowService : IPerformanceWatchingWindowService
    {
        private readonly IPerformanceWatchingService _watching;
        private readonly IPerformanceWindowsService _windows;
        private readonly IMusicService _music;
        private readonly IPerformanceRegistryService _sessions;

        public PerformanceWatchingWindowService(
            IPerformanceWatchingService watching,
            IPerformanceWindowsService windows,
            IMusicService music,
            IPerformanceRegistryService sessions)
        {
            _watching = watching;
            _windows = windows;
            _music = music;
            _sessions = sessions;
        }

        /// <summary>
        /// Picks the performer a watcher follows for that performer's kind (dancer or musician); the Watching window calls this.
        /// The choice holds while the performer stays in range, and the strongest performer is followed otherwise.
        /// </summary>
        public bool Follow(uint watcher, uint performer)
        {
            if (watcher == performer ||
                !GetIsObjectValid(watcher) ||
                !TryGetSession(performer, out var session))
                return false;

            var state = _watching.GetState(watcher);
            if (session.Kind == PerformanceKind.Dance)
                state.DancerChoice = performer;
            else
                state.MusicianChoice = performer;

            return true;
        }

        /// <summary>Drops the watcher's pick of this performer, so they go back to following the strongest of that kind.</summary>
        public bool Unfollow(uint watcher, uint performer)
        {
            if (performer == 0 || !_watching.TryGetState(watcher, out var state))
                return false;

            var cleared = false;
            if (state.DancerChoice == performer)
            {
                state.DancerChoice = 0;
                cleared = true;
            }

            if (state.MusicianChoice == performer)
            {
                state.MusicianChoice = 0;
                cleared = true;
            }

            return cleared;
        }

        /// <summary>
        /// The Watching window's content from the last audience tick, or null when the watcher is not being served.
        /// A window that opens between ticks starts from it.
        /// </summary>
        public PerformanceWatchView BuildWatchingView(uint watcher)
        {
            if (!_watching.TryGetState(watcher, out var state) || state.LastEntry == null || state.LastReport == null)
                return null;

            var entry = state.LastEntry;
            var report = state.LastReport;
            var composition = PerformanceInspiredComposer.Compose(entry.Dancer, entry.Musician, report.Plan.Venue);
            return _watching.BuildWatchView(state, entry, report, report.Plan, composition);
        }

        /// <summary>
        /// Redraws the Watching window after a Follow click. The followed performers and the banking rate change at once;
        /// the Inspired and debt numbers stay as the last tick left them.
        /// </summary>
        public void RefreshWatching(uint watcher)
        {
            if (!_watching.TryGetState(watcher, out var state) ||
                state.LastEntry == null ||
                state.LastReport == null)
                return;

            // The follow and its cap change now, not at the next tick, so a Flourish before then reaches the performer
            // just picked and banks against that performer's cap.
            var entry = state.LastEntry;
            PerformanceFollow.Apply(entry, state);
            var plan = PerformanceWatchPlan.Create(
                state.ContinuousMinutes, entry.Dancer, entry.Musician, PerformanceAudienceRules.TroupeCount(entry.Candidates));
            state.CapMinutes = plan.CapMinutes;

            if (!_windows.IsWatchingOpen(watcher))
                return;

            var composition = PerformanceInspiredComposer.Compose(entry.Dancer, entry.Musician, plan.Venue);
            _windows.ShowWatching(watcher, _watching.BuildWatchView(state, entry, state.LastReport, plan, composition));
        }

        /// <summary>
        /// A watcher who closes the Watching window keeps it closed until they start watching a new show. A window closed
        /// because they changed area, or stopped watching, is not their choice and does not count.
        /// </summary>
        public void OnWatchingWindowClosed(uint watcher)
        {
            if (_watching.TryGetState(watcher, out var state) &&
                state.WatchingArea != 0 &&
                GetArea(watcher) == state.WatchingArea)
            {
                state.WatchingDismissed = true;
            }
        }

        /// <summary>
        /// The Watching window's "Play the performer's song" box. Clearing it hands the watcher their own music back at once;
        /// checking it brings the band they follow back without waiting for the next tick.
        /// </summary>
        public void SetPlayPerformerSong(uint watcher, bool play)
        {
            GetPreference(watcher).PlayPerformerSong = play;
            if (!_watching.TryGetState(watcher, out var state))
                return;

            if (!play)
            {
                _watching.RestoreMusic(state);
                return;
            }

            int? desired = state.LastEntry?.Musician?.BandTrackId;
            if (!desired.HasValue || desired.Value < 0 || state.BandTrackId == desired || state.WatchingArea == 0)
                return;

            _music.PlayTemporaryTrack(watcher, desired.Value);
            state.BandTrackId = desired;
            state.BandArea = state.WatchingArea;
        }

        // The calls into the Performance session service, one line each so a test can answer them.

        protected virtual bool TryGetSession(uint performer, out PerformanceSession session) =>
            _sessions.TryGetSession(performer, out session);

        protected virtual PerformancePreference GetPreference(uint player) => _sessions.GetPreference(player);
    }
}

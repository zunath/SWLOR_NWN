using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The audience side of Performance: one module-wide tick a minute turns watching into banked Inspired, cleared XP
    /// debt, band music and Performance XP for the performers. The decisions live in pure classes (follow choice, watch plan,
    /// composition, Flourish payouts); this class reads the engine, applies the results and keeps what the next tick needs.
    /// The logic that used to be static in <c>PerformanceAudience</c> is moved here unchanged, and
    /// the tick schedule is this instance's own. What each watcher is doing between ticks is
    /// <see cref="IPerformanceWatchingService"/>'s, the Flourish bank is <see cref="IPerformanceFlourishService"/>'s and the Watching
    /// window's actions are <see cref="IPerformanceWatchingWindowService"/>'s.
    /// </summary>
    public class PerformanceAudienceService : IPerformanceAudienceService
    {
        private const string SkippedMessage = "Performance audience skipped a participant: PlayerId={PlayerId}";

        private readonly IDBService _db;
        private readonly IPerformanceVenueService _venue;
        private readonly IPerformanceWindowsService _windows;
        private readonly IMusicService _music;
        private readonly IPerformanceSongService _songs;
        private readonly IPerformanceSnapshotService _snapshots;
        private readonly IPerformanceInspiredService _inspired;
        private readonly IPerformanceRegistryService _sessions;
        private readonly IPerformanceWatchingService _watching;
        private readonly ISkillService _skill;
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<PerformerSnapshot> _scratchPartners = new();
        private IDisposable _tickSchedule;

        public PerformanceAudienceService(
            IDBService db,
            IPerformanceVenueService venue,
            IPerformanceWindowsService windows,
            IMusicService music,
            IPerformanceSongService songs,
            IPerformanceSnapshotService snapshots,
            IPerformanceInspiredService inspired,
            IPerformanceRegistryService sessions,
            IPerformanceWatchingService watching,
            ISkillService skill)
        {
            _db = db;
            _venue = venue;
            _windows = windows;
            _music = music;
            _songs = songs;
            _snapshots = snapshots;
            _inspired = inspired;
            _sessions = sessions;
            _watching = watching;
            _skill = skill;
        }


        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void StartAudienceTick()
        {
            // The watching is the music's listener for a replaced track. A reload rebuilds the music's list, so the boot
            // handler that starts the tick (rerun on a reload) registers it again.
            _music.RegisterMusicReplacedListener(_watching);
            _tickSchedule?.Dispose();
            _tickSchedule = ScheduleRepeating(
                OnAudienceTick, TimeSpan.FromSeconds(PerformanceAudienceRules.TickSeconds));
        }

        private void OnAudienceTick()
        {
            var profilerStart = FrameProfiler.Start();
            try
            {
                RunTick(UtcNow());
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance audience tick failed");
            }
            finally
            {
                FrameProfiler.Stop(FrameMetric.Scheduler, profilerStart, "PerformanceAudienceTick");
            }
        }

        private void RunTick(DateTime now)
        {
            _watching.NextTick();

            var sessions = GetSessions();
            if (sessions.Count == 0)
            {
                _watching.ReleaseUnseenStates();
                return;
            }

            var shows = BuildShows(sessions);
            if (shows.Count > 0)
            {
                CollectWatchers(shows);

                var compositions = new Dictionary<(uint, uint, int), InspiredEffectData>();
                foreach (var show in shows.Values)
                    ServeShow(show, compositions, now);
            }

            _watching.ReleaseUnseenStates();
        }

        private Dictionary<uint, PerformanceAreaShow> BuildShows(IReadOnlyList<PerformanceSession> sessions)
        {
            var shows = new Dictionary<uint, PerformanceAreaShow>();
            foreach (var session in sessions)
            {
                try
                {
                    AddPerformer(shows, session);
                }
                catch (Exception ex)
                {
                    Log.WriteError(ex, SkippedMessage, session.PlayerId);
                }
            }

            foreach (var show in shows.Values)
                PerformanceShow.Prepare(show.Performers);

            return shows;
        }

        private void AddPerformer(Dictionary<uint, PerformanceAreaShow> shows, PerformanceSession session)
        {
            var performer = session.Performer;
            if (!GetIsObjectValid(performer))
                return;

            var area = GetArea(performer);
            if (!GetIsObjectValid(area))
                return;

            if (!_watching.TryGetPerformerState(performer, out var performerState))
            {
                var song = session.Kind == PerformanceKind.Music
                    ? ChooseSong(session.PlayerId, GetPreference(performer).SongTrackId)
                    : -1;
                performerState = new PerformancePerformerState(session.StartedUtc, song);
                _watching.SetPerformerState(performer, performerState);
            }

            var venue = _venue.Combine(
                _venue.GetAreaRating(area), _venue.HasDanceFloorNearby(performer));
            var snapshot = BuildSnapshot(session, venue, performerState.SongTrackId);

            if (!shows.TryGetValue(area, out var show))
            {
                show = new PerformanceAreaShow(area);
                shows[area] = show;
            }

            show.Performers.Add(snapshot);
        }

        /// <summary>
        /// One pass over the online players finds every watcher: a player character in a show's area who is not performing,
        /// not in combat, within range of at least one performer, and the first of their public CD key.
        /// </summary>
        private void CollectWatchers(Dictionary<uint, PerformanceAreaShow> shows)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!shows.TryGetValue(GetArea(player), out var show))
                    continue;

                if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player) ||
                    GetIsDead(player) || GetIsInCombat(player) ||
                    TryGetSession(player, out _))
                    continue;

                var position = GetPosition(player);
                List<PerformerSnapshot> candidates = null;
                foreach (var performer in show.Performers)
                {
                    if (!PerformanceAudienceRules.IsWithinRange(position, performer.Position, performer.RangeMeters))
                        continue;

                    candidates ??= new List<PerformerSnapshot>();
                    candidates.Add(performer);
                }

                if (candidates == null || !show.CdKeys.Add(GetPCPublicCDKey(player)))
                    continue;

                show.Watchers.Add(new PerformanceWatcherEntry(player, position, candidates));
            }
        }

        private void ServeShow(
            PerformanceAreaShow show,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            foreach (var entry in show.Watchers)
            {
                var state = _watching.GetState(entry.Player);
                state.LastSeenTick = _watching.TickNumber;
                entry.State = state;
                PerformanceFollow.Apply(entry, state);

                foreach (var candidate in entry.Candidates)
                    candidate.WatcherCount++;
                if (entry.Dancer != null)
                    entry.Dancer.Crowd++;
                if (entry.Musician != null)
                    entry.Musician.Crowd++;
            }

            foreach (var entry in show.Watchers)
            {
                try
                {
                    ServeWatcher(show, entry, compositions, now);
                }
                catch (Exception ex)
                {
                    Log.WriteError(ex, SkippedMessage, GetObjectUUID(entry.Player));
                }
            }

            foreach (var performer in show.Performers)
            {
                try
                {
                    ServePerformer(show, performer, compositions, now);
                }
                catch (Exception ex)
                {
                    Log.WriteError(ex, SkippedMessage, performer.Session?.PlayerId);
                }
            }
        }

        private void ServeWatcher(
            PerformanceAreaShow show,
            PerformanceWatcherEntry entry,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            var report = new PerformanceWatcherReport();
            try
            {
                PayWatcher(show, entry, compositions, now, report);
                UpdateWatchingWindow(show, entry, compositions, report);
            }
            finally
            {
                report.DbPlayer = null;
            }
        }

        private void PayWatcher(
            PerformanceAreaShow show,
            PerformanceWatcherEntry entry,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now,
            PerformanceWatcherReport report)
        {
            var watcher = entry.Player;
            var state = entry.State;
            state.WatchingSinceUtc ??= now;
            state.ContinuousMinutes = PerformanceAudienceRules.WatchedMinutes(state.WatchingSinceUtc.Value, now);

            var plan = PerformanceWatchPlan.Create(
                state.ContinuousMinutes, entry.Dancer, entry.Musician, PerformanceAudienceRules.TroupeCount(entry.Candidates));
            state.CapMinutes = plan.CapMinutes;
            report.Plan = plan;
            UpdateBandMusic(show, entry);

            var hasEffect = IsInspired(watcher);
            var frozenSeconds = PerformanceAudienceRules.FrozenRemainingSeconds(
                hasEffect ? GetInspiredRemainingSeconds(watcher, now) : 0.0,
                hasEffect && state.HasFrozenRemaining,
                state.FrozenRemainingSeconds);
            report.RemainingSeconds = frozenSeconds;

            if (!plan.IsPaying)
            {
                // During the warm-up nothing is banked, but Inspired the watcher already holds does not run down while they watch.
                if (hasEffect)
                {
                    SetInspiredRemaining(watcher, PerformanceAudienceRules.HeldSeconds(frozenSeconds));
                    state.HasFrozenRemaining = true;
                    state.FrozenRemainingSeconds = frozenSeconds;
                }

                return;
            }

            var composition = Compose(compositions, entry.Dancer, entry.Musician, plan.Venue);
            var remainingSeconds = PerformanceAudienceRules.BankedRemainingSeconds(
                frozenSeconds, plan.BankedMinutes, plan.CapMinutes);
            report.RemainingSeconds = remainingSeconds;
            ApplyInspired(watcher, composition, remainingSeconds, plan.BankedMinutes > 0);
            state.HasFrozenRemaining = true;
            state.FrozenRemainingSeconds = remainingSeconds;

            if (!hasEffect)
                SendMessageToPC(watcher, $"The performance inspires you. (+{composition.XPBonusPercent}% skill XP while Inspired)");

            ClearDebt(watcher, plan.DebtClearPercent, report);
        }

        // At most one player read and one write per watcher per tick, and only when some debt was cleared.
        private void ClearDebt(uint watcher, int percent, PerformanceWatcherReport report)
        {
            if (percent <= 0)
                return;

            var dbPlayer = LoadPlayer(watcher, report);
            if (dbPlayer == null)
                return;

            report.XpDebt = dbPlayer.XPDebt;
            var cleared = PerformanceMath.DebtCleared(dbPlayer.XPDebt, percent);
            if (cleared <= 0)
                return;

            dbPlayer.XPDebt -= cleared;
            _db.Set(dbPlayer);
            report.XpDebt = dbPlayer.XPDebt;
            report.DebtCleared = cleared;
            SendMessageToPC(watcher, $"The performance clears {cleared} XP of your debt. (Remaining: {dbPlayer.XPDebt})");
        }

        // The one place the tick reads a watcher's player record, so the debt and the Watching window share a single read.
        private Player LoadPlayer(uint watcher, PerformanceWatcherReport report)
        {
            return report.DbPlayer ??= _db.Get<Player>(GetObjectUUID(watcher));
        }

        /// <summary>
        /// Opens the Watching window for a watcher after their first counted minute, unless they closed it for this show, and
        /// shows this tick's numbers in it. A window that is closed costs nothing beyond the open check.
        /// </summary>
        private void UpdateWatchingWindow(
            PerformanceAreaShow show,
            PerformanceWatcherEntry entry,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            PerformanceWatcherReport report)
        {
            var watcher = entry.Player;
            var state = entry.State;

            // A different show is a fresh start: a window the player closed for the last one may open again.
            if (state.WatchingArea != show.Area)
            {
                state.WatchingArea = show.Area;
                state.WatchingDismissed = false;
            }

            // The window opens itself only after the first counted minute, so walking past a show does not open it.
            var isOpen = _windows.IsWatchingOpen(watcher);
            if (!isOpen && !state.WatchingDismissed && state.ContinuousMinutes > 0)
                isOpen = _windows.OpenWatching(watcher);

            if (!isOpen)
            {
                state.LastEntry = null;
                state.LastReport = null;
                return;
            }

            if (report.XpDebt < 0)
                report.XpDebt = LoadPlayer(watcher, report)?.XPDebt ?? -1;

            // Kept so a Follow click can redraw the window from this tick's numbers without waiting for the next tick.
            report.DbPlayer = null;
            state.LastEntry = entry;
            state.LastReport = report;

            var composition = Compose(compositions, entry.Dancer, entry.Musician, report.Plan.Venue);
            _windows.ShowWatching(watcher, _watching.BuildWatchView(state, entry, report, report.Plan, composition));
        }

        private void ServePerformer(
            PerformanceAreaShow show,
            PerformerSnapshot performer,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            var state = _watching.GetState(performer.Performer);
            state.LastSeenTick = _watching.TickNumber;

            // Performing interrupts watching, as leaving range does. OnPerformanceStarted already did this; a performer
            // whose start was missed (a reload) is caught here.
            _watching.InterruptWatching(state);

            // Practice, with no watcher in range, earns no Inspired.
            if (PerformanceAudienceRules.IsLiveShow(performer.WatcherCount))
                InspirePerformer(show, performer, state, compositions, now);

            PayPerformerXp(performer, now);
            ShowAudienceInCueWindow(show, performer);
        }

        // The performer's cue window learns who is watching once a minute, here, and never from the beat loop.
        private void ShowAudienceInCueWindow(PerformanceAreaShow show, PerformerSnapshot performer)
        {
            if (performer.Session?.View is { IsOpen: true } view)
                view.ShowAudience(_windows.BuildAudienceInfo(show, performer));
        }

        // A performer in a live show is Inspired by their own part and by the other kind of performer they follow.
        // They never count toward anyone's crowd, and they hold Inspired only for a short while rather than banking it.
        private void InspirePerformer(
            PerformanceAreaShow show,
            PerformerSnapshot performer,
            PerformanceAudienceState state,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            var otherKind = performer.Kind == PerformanceKind.Dance ? PerformanceKind.Music : PerformanceKind.Dance;
            _scratchPartners.Clear();
            foreach (var other in show.Performers)
            {
                if (other.Kind == otherKind &&
                    PerformanceAudienceRules.IsWithinRange(performer.Position, other.Position, other.RangeMeters))
                    _scratchPartners.Add(other);
            }

            var otherIsDancer = otherKind == PerformanceKind.Dance;
            var followed = PerformanceFollow.Choose(
                _scratchPartners,
                otherKind,
                otherIsDancer ? state.DancerChoice : state.MusicianChoice,
                otherIsDancer ? state.FollowedDancer : state.FollowedMusician);
            if (otherIsDancer)
                state.FollowedDancer = followed?.Performer ?? 0;
            else
                state.FollowedMusician = followed?.Performer ?? 0;

            var dancer = performer.Kind == PerformanceKind.Dance ? performer : followed;
            var musician = performer.Kind == PerformanceKind.Music ? performer : followed;
            var venue = Math.Max(performer.Venue, followed?.Venue ?? 0);
            var composition = Compose(compositions, dancer, musician, venue);

            var remainingSeconds = Math.Max(
                GetInspiredRemainingSeconds(performer.Performer, now),
                PerformanceAudienceRules.PerformerInspiredSeconds);
            ApplyInspired(performer.Performer, composition, remainingSeconds, false);
        }

        // One Performance XP award per performer per tick. A first tick shorter than a minute pays only the time performed.
        private void PayPerformerXp(PerformerSnapshot performer, DateTime now)
        {
            if (!_watching.TryGetPerformerState(performer.Performer, out var performerState))
                return;

            var since = performerState.LastPaidUtc > performer.StartedUtc ? performerState.LastPaidUtc : performer.StartedUtc;
            var elapsedSeconds = Math.Clamp((now - since).TotalSeconds, 0.0, PerformanceAudienceRules.TickSeconds);
            performerState.LastPaidUtc = now;

            var xp = PerformanceMath.PerformerXP(
                PerformanceMath.PerformanceXPPerMinute, performer.Quality, performer.Crowd, performer.Tempo);
            xp = (int)Math.Round(xp * elapsedSeconds / PerformanceAudienceRules.TickSeconds, MidpointRounding.AwayFromZero);
            if (xp > 0)
                GiveSkillXP(performer.Performer, SkillType.Performance, xp);
        }

        // Sends the band's song only when the one a listener should hear actually changes.
        private void UpdateBandMusic(PerformanceAreaShow show, PerformanceWatcherEntry entry)
        {
            var state = entry.State;
            int? desired = entry.Musician?.BandTrackId;
            if (desired.HasValue && desired.Value < 0)
                desired = null;

            // A watcher who cleared "Play the performer's song" in the Watching window keeps their own music.
            if (desired.HasValue && !GetPreference(entry.Player).PlayPerformerSong)
                desired = null;

            if (desired == state.BandTrackId && (!desired.HasValue || state.BandArea == show.Area))
                return;

            if (desired.HasValue)
                _music.PlayTemporaryTrack(entry.Player, desired.Value);
            else
                _music.RestoreBackgroundMusic(entry.Player);

            state.BandTrackId = desired;
            state.BandArea = desired.HasValue ? show.Area : 0;
        }

        private InspiredEffectData Compose(
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            PerformerSnapshot dancer,
            PerformerSnapshot musician,
            int venue)
        {
            var key = (dancer?.Performer ?? 0u, musician?.Performer ?? 0u, venue);
            if (!compositions.TryGetValue(key, out var composition))
            {
                composition = PerformanceInspiredComposer.Compose(dancer, musician, venue);
                compositions[key] = composition;
            }

            return composition;
        }

        // The calls into the unconverted Scheduler and Skill statics and the Performance services, one line each so a test
        // can answer them.

        /// <summary>The time now.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual IDisposable ScheduleRepeating(Action action, TimeSpan interval) =>
            Scheduler.ScheduleRepeating(action, interval);

        protected virtual bool TryGetSession(uint performer, out PerformanceSession session) =>
            _sessions.TryGetSession(performer, out session);

        protected virtual IReadOnlyList<PerformanceSession> GetSessions() => _sessions.GetSessions();

        protected virtual PerformancePreference GetPreference(uint player) => _sessions.GetPreference(player);

        protected virtual bool IsInspired(uint creature) => _inspired.IsInspired(creature);

        protected virtual void GiveSkillXP(uint player, SkillType skill, int xp) => _skill.GiveSkillXP(player, skill, xp);

        protected virtual PerformerSnapshot BuildSnapshot(PerformanceSession session, int venue, int songTrackId) =>
            _snapshots.Build(session, venue, songTrackId);

        protected virtual int ChooseSong(string playerId, int preferredTrackId) =>
            _songs.ChooseSong(playerId, preferredTrackId);

        protected virtual bool ApplyInspired(uint creature, InspiredEffectData data, double remainingSeconds, bool bankedAgain) =>
            _inspired.Apply(creature, data, remainingSeconds, bankedAgain);

        protected virtual double GetInspiredRemainingSeconds(uint creature, DateTime nowUtc) =>
            _inspired.GetRemainingSeconds(creature, nowUtc);

        protected virtual void SetInspiredRemaining(uint creature, double remainingSeconds, bool bankedAgain = false) =>
            _inspired.SetRemaining(creature, remainingSeconds, bankedAgain);
    }
}

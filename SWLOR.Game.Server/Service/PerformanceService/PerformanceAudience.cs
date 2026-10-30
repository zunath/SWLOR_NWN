using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The audience side of Performance: one module-wide tick a minute turns watching into banked Inspired, cleared XP
    /// debt, band music and Performance XP for the performers. The decisions live in pure classes (follow choice, watch plan,
    /// composition, Flourish payouts); this class reads the engine, applies the results and keeps what the next tick needs.
    /// </summary>
    public static class PerformanceAudience
    {
        private const string SkippedMessage = "Performance audience skipped a participant: PlayerId={PlayerId}";

        private static readonly Dictionary<uint, PerformanceAudienceState> _states = new();
        private static readonly Dictionary<uint, PerformancePerformerState> _performerStates = new();
        private static readonly List<PerformerSnapshot> _scratchPartners = new();
        private static IDisposable _tickSchedule;
        private static int _tickNumber;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void StartAudienceTick()
        {
            _tickSchedule?.Dispose();
            _tickSchedule = Scheduler.ScheduleRepeating(
                OnAudienceTick, TimeSpan.FromSeconds(PerformanceAudienceRules.TickSeconds));
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetExitingPlayer()
        {
            _states.Remove(GetExitingObject());
        }

        /// <summary>
        /// Picks the performer a watcher follows for that performer's kind (dancer or musician); the Watching window calls this.
        /// The choice holds while the performer stays in range, and the strongest performer is followed otherwise.
        /// </summary>
        public static bool Follow(uint watcher, uint performer)
        {
            if (watcher == performer ||
                !GetIsObjectValid(watcher) ||
                !Performance.TryGetSession(performer, out var session))
                return false;

            var state = GetState(watcher);
            if (session.Kind == PerformanceKind.Dance)
                state.DancerChoice = performer;
            else
                state.MusicianChoice = performer;

            return true;
        }

        /// <summary>Goes back to following the strongest performer of a kind.</summary>
        public static void ClearFollow(uint watcher, PerformanceKind kind)
        {
            if (!_states.TryGetValue(watcher, out var state))
                return;

            if (kind == PerformanceKind.Dance)
                state.DancerChoice = 0;
            else
                state.MusicianChoice = 0;
        }

        /// <summary>
        /// A Flourish banks minutes for every watcher past their warm-up who follows the performer, at most once per two
        /// minutes each. Two performers who Flourish within two seconds of each other double it for both audiences.
        /// </summary>
        public static void OnFlourish(PerformanceSession session)
        {
            var now = session.LastFlourishUtc ?? DateTime.UtcNow;
            var performer = session.Performer;
            var range = GetPerformerRange(performer);
            var baseMinutes = GetFlourishBaseMinutes(session);

            var partner = FindSynchronizedFlourish(session, now, range, out var partnerBaseMinutes, out var partnerRange);
            var synchronized = partner != null;

            foreach (var state in _states.Values)
            {
                if (!state.IsWarmedUp)
                    continue;

                var watcherBase = 0;
                if (state.Follows(performer) && IsInRange(state.Creature, performer, range))
                    watcherBase = baseMinutes;
                if (synchronized && state.Follows(partner.Performer) && IsInRange(state.Creature, partner.Performer, partnerRange))
                    watcherBase = Math.Max(watcherBase, partnerBaseMinutes);

                if (watcherBase > 0)
                    BankFlourish(state, watcherBase, synchronized, now);
            }
        }

        /// <summary>
        /// A band that loses a member stops playing for the watchers who followed that member. Watchers following another
        /// band member hear the new leader's song at the next tick.
        /// </summary>
        public static void OnSessionEnded(PerformanceSession session, PerformanceEndReason reason)
        {
            var performer = session.Performer;
            _performerStates.Remove(performer);

            foreach (var state in _states.Values)
            {
                if (state.DancerChoice == performer)
                    state.DancerChoice = 0;
                if (state.MusicianChoice == performer)
                    state.MusicianChoice = 0;
                if (state.FollowedDancer == performer)
                    state.FollowedDancer = 0;

                if (state.FollowedMusician != performer)
                    continue;

                state.FollowedMusician = 0;
                if (state.BandTrackId.HasValue)
                    RestoreMusic(state);
            }
        }

        private static void OnAudienceTick()
        {
            var profilerStart = FrameProfiler.Start();
            try
            {
                RunTick(DateTime.UtcNow);
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

        private static void RunTick(DateTime now)
        {
            _tickNumber++;

            var sessions = Performance.GetSessions();
            if (sessions.Count == 0)
            {
                ReleaseUnseenStates();
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

            ReleaseUnseenStates();
        }

        private static Dictionary<uint, PerformanceAreaShow> BuildShows(IReadOnlyList<PerformanceSession> sessions)
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

        private static void AddPerformer(Dictionary<uint, PerformanceAreaShow> shows, PerformanceSession session)
        {
            var performer = session.Performer;
            if (!GetIsObjectValid(performer))
                return;

            var area = GetArea(performer);
            if (!GetIsObjectValid(area))
                return;

            if (!_performerStates.TryGetValue(performer, out var performerState))
            {
                var song = session.Kind == PerformanceKind.Music
                    ? PerformanceSongs.ChooseSong(session.PlayerId, Performance.GetPreference(performer).SongTrackId)
                    : -1;
                performerState = new PerformancePerformerState(session.StartedUtc, song);
                _performerStates[performer] = performerState;
            }

            var venue = PerformanceVenue.Combine(
                PerformanceVenue.GetAreaRating(area), PerformanceVenue.HasDanceFloorNearby(performer));
            var snapshot = PerformerSnapshotBuilder.Build(session, venue, performerState.SongTrackId);

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
        private static void CollectWatchers(Dictionary<uint, PerformanceAreaShow> shows)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!shows.TryGetValue(GetArea(player), out var show))
                    continue;

                if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player) ||
                    GetIsDead(player) || GetIsInCombat(player) ||
                    Performance.TryGetSession(player, out _))
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

        private static void ServeShow(
            PerformanceAreaShow show,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            foreach (var entry in show.Watchers)
            {
                var state = GetState(entry.Player);
                state.LastSeenTick = _tickNumber;
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

        private static void ServeWatcher(
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

        private static void PayWatcher(
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

            var existing = StatusEffect.GetStatusEffect<InspiredStatusEffect>(watcher);
            var hasEffect = existing != null;
            var frozenSeconds = PerformanceAudienceRules.FrozenRemainingSeconds(
                hasEffect ? existing.GetRemainingDurationSeconds(now) : 0.0,
                hasEffect && state.HasFrozenRemaining,
                state.FrozenRemainingSeconds);
            report.RemainingSeconds = frozenSeconds;

            if (!plan.IsPaying)
            {
                // During the warm-up nothing is banked, but Inspired the watcher already holds does not run down while they watch.
                if (hasEffect)
                {
                    PerformanceInspiredApplier.SetRemaining(watcher, PerformanceAudienceRules.HeldSeconds(frozenSeconds));
                    state.HasFrozenRemaining = true;
                    state.FrozenRemainingSeconds = frozenSeconds;
                }

                return;
            }

            var composition = Compose(compositions, entry.Dancer, entry.Musician, plan.Venue);
            var remainingSeconds = PerformanceAudienceRules.BankedRemainingSeconds(
                frozenSeconds, plan.BankedMinutes, plan.CapMinutes);
            report.RemainingSeconds = remainingSeconds;
            PerformanceInspiredApplier.Apply(watcher, composition, remainingSeconds, plan.BankedMinutes > 0);
            state.HasFrozenRemaining = true;
            state.FrozenRemainingSeconds = remainingSeconds;

            if (!hasEffect)
                SendMessageToPC(watcher, $"The performance inspires you. (+{composition.XPBonusPercent}% skill XP while Inspired)");

            ClearDebt(watcher, plan.DebtClearPercent, report);
        }

        // At most one player read and one write per watcher per tick, and only when some debt was cleared.
        private static void ClearDebt(uint watcher, int percent, PerformanceWatcherReport report)
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
            DB.Set(dbPlayer);
            report.XpDebt = dbPlayer.XPDebt;
            report.DebtCleared = cleared;
            SendMessageToPC(watcher, $"The performance clears {cleared} XP of your debt. (Remaining: {dbPlayer.XPDebt})");
        }

        // The one place the tick reads a watcher's player record, so the debt and the Watching window share a single read.
        private static Player LoadPlayer(uint watcher, PerformanceWatcherReport report)
        {
            return report.DbPlayer ??= DB.Get<Player>(GetObjectUUID(watcher));
        }

        /// <summary>
        /// Opens the Watching window for a watcher after their first counted minute, unless they closed it for this show, and
        /// shows this tick's numbers in it. A window that is closed costs nothing beyond the open check.
        /// </summary>
        private static void UpdateWatchingWindow(
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
            var isOpen = PerformanceWindows.IsWatchingOpen(watcher);
            if (!isOpen && !state.WatchingDismissed && state.ContinuousMinutes > 0)
                isOpen = PerformanceWindows.OpenWatching(watcher);

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
            PerformanceWindows.ShowWatching(watcher, BuildWatchView(state, entry, report, report.Plan, composition));
        }

        private static PerformanceWatchView BuildWatchView(
            PerformanceAudienceState state,
            PerformanceWatcherEntry entry,
            PerformanceWatcherReport report,
            PerformanceWatchPlan plan,
            InspiredEffectData composition)
        {
            var watcher = entry.Player;
            return PerformanceWatchViewBuilder.Build(
                entry.Candidates,
                entry.Dancer,
                entry.Musician,
                plan,
                state.ContinuousMinutes,
                report.RemainingSeconds,
                report.XpDebt,
                report.DebtCleared,
                composition,
                performer => state.GetName(performer, _tickNumber, () => PlayerName.GetDisplayName(watcher, performer)));
        }

        /// <summary>
        /// The Watching window's content from the last audience tick, or null when the watcher is not being served.
        /// A window that opens between ticks starts from it.
        /// </summary>
        public static PerformanceWatchView BuildWatchingView(uint watcher)
        {
            if (!_states.TryGetValue(watcher, out var state) || state.LastEntry == null || state.LastReport == null)
                return null;

            var entry = state.LastEntry;
            var report = state.LastReport;
            var composition = PerformanceInspiredComposer.Compose(entry.Dancer, entry.Musician, report.Plan.Venue);
            return BuildWatchView(state, entry, report, report.Plan, composition);
        }

        /// <summary>
        /// Redraws the Watching window after a Follow click. The followed performers and the banking rate change at once;
        /// the Inspired and debt numbers stay as the last tick left them.
        /// </summary>
        public static void RefreshWatching(uint watcher)
        {
            if (!_states.TryGetValue(watcher, out var state) ||
                state.LastEntry == null ||
                state.LastReport == null)
                return;

            // The follow changes now, not at the next tick, so a Flourish before then reaches the performer just picked.
            var entry = state.LastEntry;
            PerformanceFollow.Apply(entry, state);

            if (!PerformanceWindows.IsWatchingOpen(watcher))
                return;

            var plan = PerformanceWatchPlan.Create(
                state.ContinuousMinutes, entry.Dancer, entry.Musician, PerformanceAudienceRules.TroupeCount(entry.Candidates));
            var composition = PerformanceInspiredComposer.Compose(entry.Dancer, entry.Musician, plan.Venue);
            PerformanceWindows.ShowWatching(watcher, BuildWatchView(state, entry, state.LastReport, plan, composition));
        }

        /// <summary>
        /// A watcher who closes the Watching window keeps it closed until they start watching a new show. A window closed
        /// because they changed area, or stopped watching, is not their choice and does not count.
        /// </summary>
        public static void OnWatchingWindowClosed(uint watcher)
        {
            if (_states.TryGetValue(watcher, out var state) &&
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
        public static void SetPlayPerformerSong(uint watcher, bool play)
        {
            Performance.GetPreference(watcher).PlayPerformerSong = play;
            if (!_states.TryGetValue(watcher, out var state))
                return;

            if (!play)
            {
                RestoreMusic(state);
                return;
            }

            int? desired = state.LastEntry?.Musician?.BandTrackId;
            if (!desired.HasValue || desired.Value < 0 || state.BandTrackId == desired || state.WatchingArea == 0)
                return;

            Music.PlayTemporaryTrack(watcher, desired.Value);
            state.BandTrackId = desired;
            state.BandArea = state.WatchingArea;
        }

        /// <summary>A musician picked another song in the Perform window; the band's audience hears it at the next tick.</summary>
        public static void SetPerformerSong(uint performer, int trackId)
        {
            if (_performerStates.TryGetValue(performer, out var performerState))
                performerState.SongTrackId = trackId;
        }

        private static void ServePerformer(
            PerformanceAreaShow show,
            PerformerSnapshot performer,
            Dictionary<(uint, uint, int), InspiredEffectData> compositions,
            DateTime now)
        {
            var state = GetState(performer.Performer);
            state.LastSeenTick = _tickNumber;

            // Performing interrupts watching, as leaving range does: the warm-up starts over. Picked performers are kept.
            if (state.IsWatching)
            {
                RestoreMusic(state);
                state.StopWatching();
                PerformanceWindows.CloseWatching(performer.Performer);
            }

            // Practice, with no watcher in range, earns no Inspired.
            if (PerformanceAudienceRules.IsLiveShow(performer.WatcherCount))
                InspirePerformer(show, performer, state, compositions, now);

            PayPerformerXp(performer, now);
            ShowAudienceInCueWindow(show, performer);
        }

        // The performer's cue window learns who is watching once a minute, here, and never from the beat loop.
        private static void ShowAudienceInCueWindow(PerformanceAreaShow show, PerformerSnapshot performer)
        {
            if (performer.Session?.View is { IsOpen: true } view)
                view.ShowAudience(PerformanceWindows.BuildAudienceInfo(show, performer));
        }

        // A performer in a live show is Inspired by their own part and by the other kind of performer they follow.
        // They never count toward anyone's crowd, and they hold Inspired only for a short while rather than banking it.
        private static void InspirePerformer(
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
                PerformanceInspiredApplier.GetRemainingSeconds(performer.Performer, now),
                PerformanceAudienceRules.PerformerInspiredSeconds);
            PerformanceInspiredApplier.Apply(performer.Performer, composition, remainingSeconds, false);
        }

        // One Performance XP award per performer per tick. A first tick shorter than a minute pays only the time performed.
        private static void PayPerformerXp(PerformerSnapshot performer, DateTime now)
        {
            if (!_performerStates.TryGetValue(performer.Performer, out var performerState))
                return;

            var since = performerState.LastPaidUtc > performer.StartedUtc ? performerState.LastPaidUtc : performer.StartedUtc;
            var elapsedSeconds = Math.Clamp((now - since).TotalSeconds, 0.0, PerformanceAudienceRules.TickSeconds);
            performerState.LastPaidUtc = now;

            var xp = PerformanceMath.PerformerXP(
                PerformanceMath.PerformanceXPPerMinute, performer.Quality, performer.Crowd, performer.Tempo);
            xp = (int)Math.Round(xp * elapsedSeconds / PerformanceAudienceRules.TickSeconds, MidpointRounding.AwayFromZero);
            if (xp > 0)
                Skill.GiveSkillXP(performer.Performer, SkillType.Performance, xp);
        }

        // Sends the band's song only when the one a listener should hear actually changes.
        private static void UpdateBandMusic(PerformanceAreaShow show, PerformanceWatcherEntry entry)
        {
            var state = entry.State;
            int? desired = entry.Musician?.BandTrackId;
            if (desired.HasValue && desired.Value < 0)
                desired = null;

            // A watcher who cleared "Play the performer's song" in the Watching window keeps their own music.
            if (desired.HasValue && !Performance.GetPreference(entry.Player).PlayPerformerSong)
                desired = null;

            if (desired == state.BandTrackId && (!desired.HasValue || state.BandArea == show.Area))
                return;

            if (desired.HasValue)
                Music.PlayTemporaryTrack(entry.Player, desired.Value);
            else
                Music.RestoreBackgroundMusic(entry.Player);

            state.BandTrackId = desired;
            state.BandArea = desired.HasValue ? show.Area : 0;
        }

        /// <summary>Entering an area replaces the client's music, so the band song is sent again at the next tick.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ForgetBandSongOnAreaEnter()
        {
            ForgetBandSong(GetEnteringObject());
        }

        /// <summary>
        /// Something else replaced the player's music, such as their own music player, so the band song they follow is
        /// sent again at the next tick.
        /// </summary>
        public static void ForgetBandSong(uint player)
        {
            if (_states.TryGetValue(player, out var state))
            {
                state.BandTrackId = null;
                state.BandArea = 0;
            }
        }

        /// <summary>Death clears Inspired, so a watcher revived in range starts the warm-up over instead of resuming it.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void StopWatchingOnDeath()
        {
            if (_states.TryGetValue(GetLastPlayerDied(), out var state))
                state.StopWatching();
        }

        private static void RestoreMusic(PerformanceAudienceState state)
        {
            if (state.BandTrackId.HasValue && GetIsObjectValid(state.Creature))
                Music.RestoreBackgroundMusic(state.Creature);

            state.BandTrackId = null;
            state.BandArea = 0;
        }

        private static InspiredEffectData Compose(
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

        // A player who left every show forgets their warm-up and gets their own music back; players who picked a performer keep that pick.
        private static void ReleaseUnseenStates()
        {
            if (_states.Count == 0)
                return;

            List<uint> forgotten = null;
            foreach (var state in _states.Values)
            {
                if (state.LastSeenTick == _tickNumber)
                    continue;

                RestoreMusic(state);
                state.StopWatching();

                // After StopWatching, so the window closing is not taken for the watcher choosing to hide it.
                if (GetIsObjectValid(state.Creature))
                    PerformanceWindows.CloseWatching(state.Creature);

                if (!state.HasChoice || !GetIsObjectValid(state.Creature))
                {
                    forgotten ??= new List<uint>();
                    forgotten.Add(state.Creature);
                }
            }

            if (forgotten == null)
                return;

            foreach (var creature in forgotten)
                _states.Remove(creature);
        }

        private static PerformanceAudienceState GetState(uint creature)
        {
            if (!_states.TryGetValue(creature, out var state))
            {
                state = new PerformanceAudienceState(creature);
                _states[creature] = state;
            }

            return state;
        }

        private static int GetFlourishBaseMinutes(PerformanceSession session)
        {
            var capstone = session.Kind == PerformanceKind.Dance ? PerkType.Showstopper : PerkType.Virtuoso;
            return PerformanceAudienceRules.FlourishBaseMinutes(Perk.GetPerkLevel(session.Performer, capstone) > 0);
        }

        private static float GetPerformerRange(uint performer)
        {
            return PerformanceAudienceRules.GetRange(Stat.GetStatAdjustment(performer, StatType.PerformanceRangeBonusMeters));
        }

        private static bool IsInRange(uint watcher, uint performer, float range)
        {
            if (!GetIsObjectValid(watcher) || !GetIsObjectValid(performer) || GetArea(watcher) != GetArea(performer))
                return false;

            return PerformanceAudienceRules.IsWithinRange(GetPosition(watcher), GetPosition(performer), range);
        }

        // The other performer in this show, in range of both, who Flourished within the two seconds before this one.
        private static PerformanceSession FindSynchronizedFlourish(
            PerformanceSession session,
            DateTime now,
            float range,
            out int partnerBaseMinutes,
            out float partnerRange)
        {
            partnerBaseMinutes = 0;
            partnerRange = 0f;

            var performerPosition = GetPosition(session.Performer);
            foreach (var other in Performance.GetSessions())
            {
                if (other == session ||
                    !other.LastFlourishUtc.HasValue ||
                    other.Area != session.Area ||
                    !PerformanceAudienceRules.AreFlourishesSynchronized(now, other.LastFlourishUtc.Value) ||
                    !GetIsObjectValid(other.Performer))
                    continue;

                var otherRange = GetPerformerRange(other.Performer);
                if (!PerformanceAudienceRules.IsWithinRange(
                        performerPosition, GetPosition(other.Performer), Math.Min(range, otherRange)))
                    continue;

                partnerBaseMinutes = GetFlourishBaseMinutes(other);
                partnerRange = otherRange;
                return other;
            }

            return null;
        }

        private static void BankFlourish(PerformanceAudienceState state, int baseMinutes, bool synchronized, DateTime now)
        {
            var minutes = PerformanceAudienceRules.FlourishMinutesToBank(
                baseMinutes, synchronized, state.LastFlourishBankUtc, state.LastFlourishBankedMinutes, now);
            if (minutes <= 0)
                return;

            // A watcher with no Inspired yet has banked nothing to add to; the next tick pays them.
            var watcher = state.Creature;
            if (!GetIsObjectValid(watcher) || StatusEffect.GetStatusEffect<InspiredStatusEffect>(watcher) == null)
                return;

            var frozenSeconds = PerformanceAudienceRules.FrozenRemainingSeconds(
                PerformanceInspiredApplier.GetRemainingSeconds(watcher, now),
                state.HasFrozenRemaining,
                state.FrozenRemainingSeconds);
            var remainingSeconds = PerformanceAudienceRules.BankedRemainingSeconds(frozenSeconds, minutes, state.CapMinutes);
            PerformanceInspiredApplier.SetRemaining(watcher, remainingSeconds, bankedAgain: remainingSeconds > frozenSeconds);

            state.HasFrozenRemaining = true;
            state.FrozenRemainingSeconds = remainingSeconds;
            state.LastFlourishBankedMinutes = PerformanceAudienceRules.FlourishBankedTotal(
                state.LastFlourishBankUtc, state.LastFlourishBankedMinutes, minutes, now);
            state.LastFlourishBankUtc = now;

            var addedMinutes = (remainingSeconds - frozenSeconds) / 60.0;
            if (addedMinutes > 0.0)
                SendMessageToPC(watcher, $"The flourish thrills you. (+{addedMinutes:0.#} min of Inspired)");
        }
    }
}

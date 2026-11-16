using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Vector3 = System.Numerics.Vector3;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Dance and Play Instrument performances: the beat loop, the session and the end triggers. The logic
    /// that used to be static in <c>Performance</c> is moved here unchanged. The live sessions and the players' preferences
    /// are <see cref="IPerformanceRegistryService"/>'s, the hot bar is <see cref="IPerformanceHotbarService"/>'s, the clips are
    /// <see cref="IPerformanceAnimationService"/>'s and the checks for starting are <see cref="IPerformanceEligibilityService"/>'s.
    /// The event handlers run on the container's instance; static callers reach it through <c>GameServices.Performance</c>.
    /// </summary>
    public class PerformanceService : IPerformanceService
    {
        private readonly IPerformanceRegistryService _sessions;
        private readonly IPerformanceWindowsService _windows;
        private readonly IPerformanceWatchingService _watching;
        private readonly IPerformanceFlourishService _flourish;
        private readonly IPerformanceSongService _songs;
        private readonly IPerformanceRiderService _riders;
        private readonly IPerformanceHotbarService _hotbar;
        private readonly IPerformanceEligibilityService _eligibility;
        private readonly IPerformanceAnimationService _animation;
        private readonly IActivityService _activity;

        public PerformanceService(
            IPerformanceRegistryService sessions,
            IPerformanceWindowsService windows,
            IPerformanceWatchingService watching,
            IPerformanceFlourishService flourish,
            IPerformanceSongService songs,
            IPerformanceRiderService riders,
            IPerformanceHotbarService hotbar,
            IPerformanceEligibilityService eligibility,
            IPerformanceAnimationService animation,
            IActivityService activity)
        {
            _sessions = sessions;
            _windows = windows;
            _watching = watching;
            _flourish = flourish;
            _songs = songs;
            _riders = riders;
            _hotbar = hotbar;
            _eligibility = eligibility;
            _animation = animation;
            _activity = activity;
        }

        public void ToggleDance(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Dance, level);
        }

        public void TogglePlayInstrument(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Music, level);
        }

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        public bool IsModeFeat(uint activator, FeatType feat)
        {
            return _hotbar.IsModeFeat(feat) || _sessions.IsPerforming(activator) && _hotbar.IsToggleFeat(feat);
        }

        public bool SetRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!_riders.IsRoutineUnlocked(player, routine))
                return false;

            _sessions.GetPreference(player).Routine = routine;
            if (_sessions.TryGetSession(player, out var session) && session.Kind == PerformanceKind.Dance)
            {
                session.Routine = routine;
                _animation.StartLoopingAnimation(session);
                _windows.RefreshSetup(session);
            }

            return true;
        }

        public bool SetSet(uint player, PerformanceSetType set)
        {
            if (!_riders.IsSetUnlocked(player, set))
                return false;

            _sessions.GetPreference(player).Set = set;
            if (_sessions.TryGetSession(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                session.Set = set;
                _windows.RefreshSetup(session);
            }

            return true;
        }

        /// <summary>Picks the blended second routine. Only a dancer with Showstopper can use it.</summary>
        public bool SetSecondRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!_riders.CanBlendSecondRoutine(player, routine))
                return false;

            _sessions.GetPreference(player).SecondRoutine = routine;
            if (_sessions.TryGetSession(player, out var session) && session.Kind == PerformanceKind.Dance)
                _windows.RefreshSetup(session);

            return true;
        }

        /// <summary>Picks the woven second set. Only a musician with Virtuoso can use it.</summary>
        public bool SetSecondSet(uint player, PerformanceSetType set)
        {
            if (!_riders.CanWeaveSecondSet(player, set))
                return false;

            _sessions.GetPreference(player).SecondSet = set;
            if (_sessions.TryGetSession(player, out var session) && session.Kind == PerformanceKind.Music)
                _windows.RefreshSetup(session);

            return true;
        }

        /// <summary>
        /// Picks the Cantina song a musician plays. The song must be one they own (or the starter song). A band's audience
        /// hears the new song at the next audience tick when the musician leads the band.
        /// </summary>
        public bool SetSong(uint player, int trackId)
        {
            if (ChooseSong(GetObjectUUID(player), trackId) != trackId)
                return false;

            _sessions.GetPreference(player).SongTrackId = trackId;
            if (_sessions.TryGetSession(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                _watching.SetPerformerSong(player, trackId);
                _windows.RefreshSetup(session);
            }

            return true;
        }

        /// <summary>
        /// Switches between active play and Roleplay mode. Roleplay mode drops the cues and Flourish from the hot bar, scores
        /// the show at the Rough rate, and keeps it going while the performer talks or emotes.
        /// </summary>
        public void SetRoleplay(uint player, bool roleplay)
        {
            _sessions.GetPreference(player).Roleplay = roleplay;
            if (!_sessions.TryGetSession(player, out var session) || session.IsRoleplay == roleplay)
                return;

            session.SetRoleplay(roleplay, UtcNow());
            _hotbar.ApplyModeQuickbar(player, session.Kind, session.ToggleFeat, roleplay);

            if (session.View is { IsOpen: true } view)
                view.ShowMode(roleplay);
            else if (!roleplay && !_windows.OpenCueWindow(session))
                ShowFallbackBeat(session);

            if (roleplay)
                _windows.ClearFallbackStrip(player);

            _windows.RefreshSetup(session);
            SendMessageToPC(player, PerformanceMessageText.GetModeMessage(session.Kind, roleplay));
        }

        public void SetTempo(uint player, PerformanceTempo tempo)
        {
            _sessions.GetPreference(player).Tempo = tempo;
            if (_sessions.TryGetSession(player, out var session))
                ApplyTempo(session, tempo);
        }

        public void HandleInput(uint activator, FeatType feat)
        {
            if (!_sessions.TryGetSession(activator, out var session))
            {
                // A mode feat with no live session is left over from an interrupted performance.
                _hotbar.RecoverInterruptedPerformance(activator);
                _hotbar.RemoveModeFeats(activator);
                return;
            }

            if (_hotbar.IsToggleFeat(feat))
            {
                EndPerformance(activator, PerformanceEndReason.Stopped);
                return;
            }

            // Only cue presses and a Flourish that fires count as input for the idle timeout; Options does not. In Roleplay
            // mode the hot bar has no cues or Flourish, and talking or emoting keeps the show going instead.
            var now = UtcNow();
            if (feat == FeatType.PerformanceFlourish)
            {
                TryFlourish(session, now);
                return;
            }

            if (feat == FeatType.PerformanceOptions)
            {
                _windows.ToggleOptions(session);
                return;
            }

            if (!_hotbar.TryGetCue(feat, out var cue) || !PerformanceCueGenerator.GetLanes(session.Kind).Contains(cue))
                return;

            if (session.RegisterCue(cue, now) == PerformanceInputResult.Hit)
                _animation.PlayHitAnimation(session, cue);
        }

        public void EndPerformance(uint player, PerformanceEndReason reason)
        {
            if (!_sessions.Remove(player, out var session))
                return;

            session.BeatSchedule?.Dispose();
            session.BeatSchedule = null;
            _windows.CloseCueWindow(session, reason);

            if (GetIsObjectValid(player))
            {
                if (reason != PerformanceEndReason.LoggedOut)
                    _windows.ClearFallbackStrip(player);

                _hotbar.RestorePlayer(player);
                ApplyHornFit(player);
                if (GetBusyType(player) == ActivityStatusType.Performing)
                    ClearBusy(player);

                if (reason != PerformanceEndReason.LoggedOut)
                {
                    if (reason != PerformanceEndReason.Died)
                        _animation.ReleaseAnimation(session, reason);

                    var message = PerformanceMessageText.GetEndMessage(reason, session.IsRoleplay);
                    if (!string.IsNullOrEmpty(message))
                        SendMessageToPC(player, message);
                }
            }

            try
            {
                _watching.OnSessionEnded(session, reason);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance audience cleanup failed: PlayerId={PlayerId}", session.PlayerId);
            }
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "Repairs a hotbar a crash left swapped at login; on a player who stays online it would overwrite the hotbar. The quiescence step ends a performance cleanly instead.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void RecoverOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            _hotbar.RecoverInterruptedPerformance(player, atLogin: true);
        }

        /// <summary>Talking or emoting keeps a Roleplay mode performance going. Chat commands do not count.</summary>
        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public void KeepRoleplayGoing()
        {
            if (_sessions.Count == 0)
                return;

            var channel = ChatPlugin.GetChannel();
            if (channel != ChatChannel.PlayerTalk &&
                channel != ChatChannel.PlayerWhisper &&
                channel != ChatChannel.PlayerParty)
                return;

            if (!_sessions.TryGetSession(ChatPlugin.GetSender(), out var session) || !session.IsRoleplay)
                return;

            var message = ChatPlugin.GetMessage()?.Trim() ?? string.Empty;
            if (message.Length == 0 || message.StartsWith('/'))
                return;

            session.NoteActivity(UtcNow());
        }

        [PlayerSession(PlayerReload.Quiesce,
            "A performance (song, beat timer) cannot be carried, so it ends as at logout: the hotbar is restored and the mode feats removed instead of staying swapped.",
            Step = nameof(EndOnLogoutFor))]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void EndOnLogout()
        {
            EndOnLogoutFor(GetExitingObject());
        }

        /// <summary>
        /// The work of <see cref="EndOnLogout"/> for <paramref name="player"/>: the logout's handler, and a hot reload's quiescence step for a player who stays online.
        /// </summary>
        public void EndOnLogoutFor(uint player)
        {
            EndPerformance(player, PerformanceEndReason.LoggedOut);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void EndOnAreaExit()
        {
            EndPerformance(GetExitingObject(), PerformanceEndReason.AreaExit);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void EndOnDeath()
        {
            EndPerformance(GetLastPlayerDied(), PerformanceEndReason.Died);
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public void EndOnDamage()
        {
            EndPerformance(OBJECT_SELF, PerformanceEndReason.Combat);
        }

        [NWNEventHandler(ScriptName.OnInputAttackObjectBefore)]
        public void EndOnAttack()
        {
            EndPerformance(OBJECT_SELF, PerformanceEndReason.Combat);
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public void EndOnInstrumentUnequipped()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!_sessions.TryGetSession(player, out var session) || session.Kind != PerformanceKind.Music)
                return;

            if (!string.IsNullOrEmpty(GetLocalString(GetPCItemLastUnequipped(), PerformanceEligibilityService.InstrumentFamilyVariable)))
                EndPerformance(player, PerformanceEndReason.InstrumentRemoved);
        }

        private void Toggle(uint player, PerformanceKind kind, int level)
        {
            if (_sessions.IsPerforming(player))
            {
                EndPerformance(player, PerformanceEndReason.Stopped);
                return;
            }

            var error = _eligibility.GetStartError(player, kind, out var family, out var instrumentSlot, out var instrument);
            if (error != null)
            {
                SendMessageToPC(player, error);
                return;
            }

            StartPerformance(player, kind, level, family, instrumentSlot, instrument);
        }

        private void StartPerformance(
            uint player,
            PerformanceKind kind,
            int level,
            PerformanceInstrumentFamily family,
            InventorySlot instrumentSlot,
            uint instrument)
        {
            var playerId = GetObjectUUID(player);

            // A hot bar saved by an interrupted performance is the player's real one; restore it before saving again.
            _hotbar.RecoverInterruptedPerformance(player);

            if (!_hotbar.TrySaveHotbar(player))
            {
                SendMessageToPC(player, "You cannot perform right now.");
                return;
            }

            var toggleFeat = _hotbar.GetToggleFeat(kind, level);

            _hotbar.AddModeFeats(player, kind);

            var preference = _sessions.GetPreference(player);
            _hotbar.ApplyModeQuickbar(player, kind, toggleFeat, preference.Roleplay);
            SetBusy(player, ActivityStatusType.Performing);

            var now = UtcNow();
            var session = new PerformanceSession(
                player, kind, family, preference.Tempo, now, new System.Random(HashCode.Combine(player, now.Ticks)))
            {
                PlayerId = playerId,
                Area = GetArea(player),
                StartPosition = GetPosition(player),
                ToggleFeat = toggleFeat,
                PerkLevel = level,
                InstrumentSlot = instrumentSlot,
                Instrument = instrument,
                InstrumentBow = PerformanceInstrumentCatalog.RequiresBow(family)
                    ? GetItemInSlot(InventorySlot.RightHand, player)
                    : OBJECT_INVALID,
                Routine = _riders.IsRoutineUnlocked(player, preference.Routine)
                    ? preference.Routine
                    : PerformanceRoutineType.CantinaShuffle,
                Set = _riders.IsSetUnlocked(player, preference.Set)
                    ? preference.Set
                    : PerformanceSetType.CantinaStandards
            };

            session.SetRoleplay(preference.Roleplay, now);

            _sessions.Add(session);
            ApplyHornFit(player);
            _watching.OnPerformanceStarted(player);
            // The toggle has no cast window to play its clip in, so the performance opens with it.
            _animation.PlayThenLoop(
                session,
                PerformanceAnimations.GetOpeningClip(kind, family),
                PerformanceAnimations.GetOpeningHoldMode(kind, family, GetResRef(instrument)));
            ScheduleBeats(session);

            SendMessageToPC(player, PerformanceMessageText.GetStartMessage(kind, session.IsRoleplay, family));

            // The cue window shows the strip; floating text stands in only when it could not be opened. Roleplay mode has no
            // strip to stand in for.
            if (!_windows.OpenCueWindow(session) && !session.IsRoleplay)
                ShowFallbackBeat(session);
        }

        private void ScheduleBeats(PerformanceSession session)
        {
            session.BeatSchedule?.Dispose();
            session.BeatStep = 0;
            session.BeatSchedule = ScheduleRepeating(
                () => OnBeat(session),
                TimeSpan.FromSeconds(session.Tempo.GetBeatSeconds() / session.StepsPerBeat));
        }

        private void OnBeat(PerformanceSession session)
        {
            var player = session.Performer;
            var profilerStart = FrameProfiler.Start();
            try
            {
                if (!_sessions.TryGetSession(player, out var live) || live != session)
                {
                    session.BeatSchedule?.Dispose();
                    return;
                }

                // A dance splits each beat into steps so its window can show the time left on the beat; a step
                // before the last pushes the bar and nothing else.
                session.BeatStep++;
                if (session.BeatStep < session.StepsPerBeat)
                {
                    if (session.View is { IsOpen: true, WantsBeatProgress: true } view)
                        view.ShowBeatProgress(1f - session.BeatStep / (float)session.StepsPerBeat);

                    return;
                }

                session.BeatStep = 0;
                var endReason = GetEndTrigger(session);
                if (endReason.HasValue)
                {
                    EndPerformance(player, endReason.Value);
                    return;
                }

                if (UtcNow() >= session.AnimationRenewUtc)
                    _animation.StartLoopingAnimation(session);

                // Roleplay mode scores nothing and shows no strip; the loop only keeps the clip going and runs the end checks.
                if (session.IsRoleplay)
                    return;

                var outcome = session.AdvanceBeat();
                PresentBeat(session, outcome);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance beat failed: PlayerId={PlayerId}", session.PlayerId);
                try
                {
                    EndPerformance(player, PerformanceEndReason.Invalid);
                }
                catch (Exception endEx)
                {
                    Log.WriteError(endEx, "Performance cleanup failed: PlayerId={PlayerId}", session.PlayerId);
                    _sessions.Remove(player, out _);
                    session.BeatSchedule?.Dispose();
                }
            }
            finally
            {
                FrameProfiler.Stop(FrameMetric.Scheduler, profilerStart, "PerformanceBeat");
            }
        }

        private PerformanceEndReason? GetEndTrigger(PerformanceSession session)
        {
            var player = session.Performer;
            if (!GetIsObjectValid(player))
                return PerformanceEndReason.Invalid;
            if (GetIsDead(player) || GetCurrentHitPoints(player) <= 0)
                return PerformanceEndReason.Died;
            if (GetArea(player) != session.Area)
                return PerformanceEndReason.AreaExit;
            if (GetIsInCombat(player))
                return PerformanceEndReason.Combat;
            if (Vector3.Distance(GetPosition(player), session.StartPosition) > PerformanceMath.MaxMoveDistanceMeters)
                return PerformanceEndReason.Moved;
            if (session.Kind == PerformanceKind.Music && !_eligibility.IsInstrumentStillEquipped(session))
                return PerformanceEndReason.InstrumentRemoved;
            if (session.IsIdle(UtcNow()))
                return PerformanceEndReason.Idle;

            return null;
        }

        private void ApplyTempo(PerformanceSession session, PerformanceTempo tempo)
        {
            session.Tempo = tempo;
            ScheduleBeats(session);
            _windows.RefreshSetup(session);
            SendMessageToPC(
                session.Performer,
                $"Tempo: {tempo} ({tempo.GetBeatSeconds():0.0}s beats, x{tempo.GetXPMultiplier():0.##} XP).");
        }

        private void TryFlourish(PerformanceSession session, DateTime now)
        {
            var performer = session.Performer;
            if (!session.TryConsumeFlourish(now))
            {
                SendMessageToPC(performer, $"Flourish is not ready ({session.FlourishMeter}/{PerformanceMath.FlourishMeterMax}).");
                return;
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Improve_Ability_Score), performer);
            _animation.PlayThenLoop(session, PerformanceAnimations.GetFlourishClip(session.Kind, session.InstrumentFamily, session.FlourishesPlayed));
            session.FlourishesPlayed++;
            SendMessageToPC(performer, "Flourish!");
            _flourish.OnFlourish(session);
        }

        // The cue window takes only what changed; with it closed, one line of screen text keeps the performance playable.
        private void PresentBeat(PerformanceSession session, PerformanceBeatOutcome outcome)
        {
            if (session.View is { IsOpen: true } view)
            {
                view.ShowBeat(session, outcome);
                return;
            }

            ShowFallbackBeat(session);
        }

        private void ShowFallbackBeat(PerformanceSession session)
        {
            var strip = string.Join(" | ", new[] { session.Current }.Concat(session.Upcoming)
                .Select((beat, index) => DescribeBeat(beat, index == 0)));
            _windows.ShowFallbackStrip(
                session.Performer,
                $"{strip}  [{session.Quality}] Streak {session.Streak} Flourish {session.FlourishMeter}/{PerformanceMath.FlourishMeterMax}",
                (float)session.Tempo.GetBeatSeconds());
        }

        private string DescribeBeat(PerformanceBeat beat, bool isCurrent)
        {
            var text = beat.IsRest
                ? "hold"
                : beat.IsChord ? $"{beat.Primary}+{beat.Second}" : beat.Primary.ToString();
            return isCurrent ? $"[{text.ToUpperInvariant()}]" : text;
        }

        // The calls into the unconverted Scheduler static, the Activity and Song services and the clock, one line each so a
        // test can answer them.

        /// <summary>The time now.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual void ApplyHornFit(uint player) =>
            Feature.AppearanceDefinition.ItemAppearance.HornFit.Apply(player);

        protected virtual IDisposable ScheduleRepeating(Action task, TimeSpan interval) =>
            Scheduler.ScheduleRepeating(task, interval);

        protected virtual ActivityStatusType GetBusyType(uint target) => _activity.GetBusyType(target);

        protected virtual void SetBusy(uint target, ActivityStatusType type) => _activity.SetBusy(target, type);

        protected virtual void ClearBusy(uint target) => _activity.ClearBusy(target);

        protected virtual int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong) =>
            _songs.ChooseSong(playerId, preferredTrackId);
    }
}

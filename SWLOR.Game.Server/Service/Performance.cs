using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.FrameProfilerService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Performance
    {
        private const string AllowNoInstrumentVariable = "PERFORMANCE_ALLOW_NO_INSTRUMENT";
        private const string InstrumentFamilyVariable = "PERFORMANCE_INSTRUMENT_FAMILY";
        // One named playback lasts at most ten minutes; the beat loop restarts the routine a minute early.
        private const float LoopPlaybackSeconds = 600f;
        private const float LoopRenewSeconds = 540f;
        // If an accent's hand-back is lost, the beat loop resumes the routine this long after the accent ends.
        private const float AccentRecoverySeconds = 1f;

        private const int FirstBarSlotCount = 12;
        private const int FlourishSlot = 4;
        private const int OptionsSlot = 5;
        private const int StopSlot = 6;

        private static readonly Dictionary<uint, PerformanceSession> _sessions = new();
        private static readonly Dictionary<string, PerformancePreference> _preferences = new();

        private static readonly FeatType[] _danceToggleFeats =
            { FeatType.Dance1, FeatType.Dance2, FeatType.Dance3, FeatType.Dance4 };

        private static readonly FeatType[] _musicToggleFeats =
            { FeatType.PlayInstrument1, FeatType.PlayInstrument2, FeatType.PlayInstrument3, FeatType.PlayInstrument4 };

        private static readonly FeatType[] _danceCueFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight
        };

        private static readonly FeatType[] _musicCueFeats =
        {
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh
        };

        private static readonly FeatType[] _allModeFeats =
            _danceCueFeats.Concat(_musicCueFeats)
                .Concat(new[] { FeatType.PerformanceFlourish, FeatType.PerformanceOptions }).ToArray();

        public static void ToggleDance(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Dance, level);
        }

        public static void TogglePlayInstrument(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Music, level);
        }

        public static bool IsModeFeat(FeatType feat)
        {
            return Array.IndexOf(_allModeFeats, feat) >= 0;
        }

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        public static bool IsModeFeat(uint activator, FeatType feat)
        {
            return IsModeFeat(feat) || _sessions.ContainsKey(activator) && IsToggleFeat(feat);
        }

        public static bool TryGetSession(uint performer, out PerformanceSession session)
        {
            return _sessions.TryGetValue(performer, out session);
        }

        public static IReadOnlyList<PerformanceSession> GetSessions()
        {
            return _sessions.Values.ToArray();
        }

        public static PerformancePreference GetPreference(uint player)
        {
            var playerId = GetObjectUUID(player);
            if (!_preferences.TryGetValue(playerId, out var preference))
            {
                preference = new PerformancePreference();
                _preferences[playerId] = preference;
            }

            return preference;
        }

        public static bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine)
        {
            return Perk.GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(routine)) > 0;
        }

        public static bool IsSetUnlocked(uint player, PerformanceSetType set)
        {
            return Perk.GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(set)) > 0;
        }

        public static bool SetRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!IsRoutineUnlocked(player, routine))
                return false;

            GetPreference(player).Routine = routine;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Dance)
            {
                session.Routine = routine;
                StartLoopingAnimation(session);
                PerformanceWindows.RefreshSetup(session);
            }

            return true;
        }

        public static bool SetSet(uint player, PerformanceSetType set)
        {
            if (!IsSetUnlocked(player, set))
                return false;

            GetPreference(player).Set = set;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                session.Set = set;
                PerformanceWindows.RefreshSetup(session);
            }

            return true;
        }

        /// <summary>Picks the blended second routine. Only a dancer with Showstopper can use it.</summary>
        public static bool SetSecondRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!IsRoutineUnlocked(player, routine) || Perk.GetPerkLevel(player, PerkType.Showstopper) <= 0)
                return false;

            GetPreference(player).SecondRoutine = routine;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Dance)
                PerformanceWindows.RefreshSetup(session);

            return true;
        }

        /// <summary>Picks the woven second set. Only a musician with Virtuoso can use it.</summary>
        public static bool SetSecondSet(uint player, PerformanceSetType set)
        {
            if (!IsSetUnlocked(player, set) || Perk.GetPerkLevel(player, PerkType.Virtuoso) <= 0)
                return false;

            GetPreference(player).SecondSet = set;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
                PerformanceWindows.RefreshSetup(session);

            return true;
        }

        /// <summary>
        /// Picks the Cantina song a musician plays. The song must be one they own (or the starter song). A band's audience
        /// hears the new song at the next audience tick when the musician leads the band.
        /// </summary>
        public static bool SetSong(uint player, int trackId)
        {
            if (PerformanceSongs.ChooseSong(GetObjectUUID(player), trackId) != trackId)
                return false;

            GetPreference(player).SongTrackId = trackId;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                PerformanceAudience.SetPerformerSong(player, trackId);
                PerformanceWindows.RefreshSetup(session);
            }

            return true;
        }

        public static void SetTempo(uint player, PerformanceTempo tempo)
        {
            GetPreference(player).Tempo = tempo;
            if (_sessions.TryGetValue(player, out var session))
                ApplyTempo(session, tempo);
        }

        /// <summary>
        /// Whether the player may play music right now and which instrument family they are using.
        /// An equipped instrument from <see cref="PerformanceInstrumentCatalog"/> sets the family. For tests, the module or player
        /// local int PERFORMANCE_ALLOW_NO_INSTRUMENT lets anyone play; a local string PERFORMANCE_INSTRUMENT_FAMILY on the player or
        /// module then picks the family (Strings by default).
        /// </summary>
        public static bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family)
        {
            return TryFindInstrument(player, out family, out _, out _);
        }

        // Found once when a performance starts; each beat then checks only that slot (see IsInstrumentStillEquipped).
        private static bool TryFindInstrument(
            uint player,
            out PerformanceInstrumentFamily family,
            out InventorySlot instrumentSlot,
            out uint instrument)
        {
            for (var slot = InventorySlot.Head; slot <= InventorySlot.Belt; slot++)
            {
                var item = GetItemInSlot(slot, player);
                if (GetIsObjectValid(item) &&
                    (PerformanceInstrumentCatalog.TryGetFamily(GetResRef(item), out family) ||
                     TryParseFamily(GetLocalString(item, InstrumentFamilyVariable), out family)))
                {
                    instrumentSlot = slot;
                    instrument = item;
                    return true;
                }
            }

            instrumentSlot = InventorySlot.Invalid;
            instrument = OBJECT_INVALID;

            var module = GetModule();
            if (GetLocalInt(module, AllowNoInstrumentVariable) != 0 || GetLocalInt(player, AllowNoInstrumentVariable) != 0)
            {
                if (!TryParseFamily(GetLocalString(player, InstrumentFamilyVariable), out family) &&
                    !TryParseFamily(GetLocalString(module, InstrumentFamilyVariable), out family))
                {
                    family = PerformanceInstrumentFamily.Strings;
                }

                return true;
            }

            family = PerformanceInstrumentFamily.None;
            return false;
        }

        public static void HandleInput(uint activator, FeatType feat)
        {
            if (!_sessions.TryGetValue(activator, out var session))
            {
                // A mode feat with no live session is left over from an interrupted performance.
                RecoverInterruptedPerformance(activator);
                RemoveModeFeats(activator);
                return;
            }

            if (IsToggleFeat(feat))
            {
                EndPerformance(activator, PerformanceEndReason.Stopped);
                return;
            }

            // Only cue presses and a Flourish that fires count as input for the idle timeout; Options does not.
            var now = DateTime.UtcNow;
            if (feat == FeatType.PerformanceFlourish)
            {
                TryFlourish(session, now);
                return;
            }

            if (feat == FeatType.PerformanceOptions)
            {
                PerformanceWindows.ToggleOptions(session);
                return;
            }

            if (!TryGetCue(feat, out var cue) || !PerformanceCueGenerator.GetLanes(session.Kind).Contains(cue))
                return;

            if (session.RegisterCue(cue, now) == PerformanceInputResult.Hit)
                PlayHitAnimation(session, cue);
        }

        public static void EndPerformance(uint player, PerformanceEndReason reason)
        {
            if (!_sessions.Remove(player, out var session))
                return;

            session.BeatSchedule?.Dispose();
            session.BeatSchedule = null;
            PerformanceWindows.CloseCueWindow(session, reason);

            if (GetIsObjectValid(player))
            {
                if (reason != PerformanceEndReason.LoggedOut)
                    PerformanceWindows.ClearFallbackStrip(player);

                RestorePlayer(player);
                if (Activity.GetBusyType(player) == ActivityStatusType.Performing)
                    Activity.ClearBusy(player);

                if (reason != PerformanceEndReason.LoggedOut)
                {
                    if (reason != PerformanceEndReason.Died)
                        ReleaseAnimation(session, reason);

                    var message = GetEndMessage(reason);
                    if (!string.IsNullOrEmpty(message))
                        SendMessageToPC(player, message);
                }
            }

            try
            {
                PerformanceAudience.OnSessionEnded(session, reason);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance audience cleanup failed: PlayerId={PlayerId}", session.PlayerId);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            RecoverInterruptedPerformance(player);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void EndOnLogout()
        {
            EndPerformance(GetExitingObject(), PerformanceEndReason.LoggedOut);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void EndOnAreaExit()
        {
            EndPerformance(GetExitingObject(), PerformanceEndReason.AreaExit);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void EndOnDeath()
        {
            EndPerformance(GetLastPlayerDied(), PerformanceEndReason.Died);
        }

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void EndOnDamage()
        {
            EndPerformance(OBJECT_SELF, PerformanceEndReason.Combat);
        }

        [NWNEventHandler(ScriptName.OnInputAttackObjectBefore)]
        public static void EndOnAttack()
        {
            EndPerformance(OBJECT_SELF, PerformanceEndReason.Combat);
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void EndOnInstrumentUnequipped()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!_sessions.TryGetValue(player, out var session) || session.Kind != PerformanceKind.Music)
                return;

            if (!string.IsNullOrEmpty(GetLocalString(GetPCItemLastUnequipped(), InstrumentFamilyVariable)))
                EndPerformance(player, PerformanceEndReason.InstrumentRemoved);
        }

        private static void Toggle(uint player, PerformanceKind kind, int level)
        {
            if (_sessions.ContainsKey(player))
            {
                EndPerformance(player, PerformanceEndReason.Stopped);
                return;
            }

            var family = PerformanceInstrumentFamily.None;
            var instrumentSlot = InventorySlot.Invalid;
            var instrument = OBJECT_INVALID;
            string error = null;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                error = "Only players may perform.";
            else if (GetIsDead(player) || GetCurrentHitPoints(player) <= 0)
                error = "You cannot perform right now.";
            else if (GetIsInCombat(player))
                error = "You cannot perform while in combat.";
            else if (Activity.IsBusy(player))
                error = "You are busy.";
            else if (Space.IsPlayerInSpaceMode(player))
                error = "You cannot perform in space.";
            else if (kind == PerformanceKind.Music && !TryFindInstrument(player, out family, out instrumentSlot, out instrument))
                error = "You must equip an instrument to play music.";

            if (error != null)
            {
                SendMessageToPC(player, error);
                return;
            }

            StartPerformance(player, kind, level, family, instrumentSlot, instrument);
        }

        private static void StartPerformance(
            uint player,
            PerformanceKind kind,
            int level,
            PerformanceInstrumentFamily family,
            InventorySlot instrumentSlot,
            uint instrument)
        {
            var playerId = GetObjectUUID(player);

            // A hot bar saved by an interrupted performance is the player's real one; restore it before saving again.
            RecoverInterruptedPerformance(player);

            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
            {
                SendMessageToPC(player, "You cannot perform right now.");
                return;
            }

            var toggleFeats = kind == PerformanceKind.Dance ? _danceToggleFeats : _musicToggleFeats;
            var toggleFeat = toggleFeats[Math.Clamp(level, 1, toggleFeats.Length) - 1];

            dbPlayer.PerformanceSavedHotBar = CreaturePlugin.SerializeQuickbar(player) ?? string.Empty;
            DB.Set(dbPlayer);

            foreach (var feat in GetModeFeats(kind))
                CreaturePlugin.AddFeat(player, feat);

            ApplyModeQuickbar(player, kind, toggleFeat);
            Activity.SetBusy(player, ActivityStatusType.Performing);

            var preference = GetPreference(player);
            var now = DateTime.UtcNow;
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
                Routine = IsRoutineUnlocked(player, preference.Routine)
                    ? preference.Routine
                    : PerformanceRoutineType.CantinaShuffle,
                Set = IsSetUnlocked(player, preference.Set)
                    ? preference.Set
                    : PerformanceSetType.CantinaStandards
            };

            _sessions[player] = session;
            // The toggle has no cast window to play its clip in, so the performance opens with it.
            PlayThenLoop(session, PerformanceAnimations.GetOpeningClip(kind));
            ScheduleBeats(session);

            SendMessageToPC(player, kind == PerformanceKind.Dance
                ? "You begin to dance. Use the step buttons on your hot bar to match the cues."
                : "You begin to play. Use the note buttons on your hot bar to match the cues.");

            // The cue window shows the strip; floating text stands in only when it could not be opened.
            if (!PerformanceWindows.OpenCueWindow(session))
                ShowFallbackBeat(session);
        }

        private static void ScheduleBeats(PerformanceSession session)
        {
            session.BeatSchedule?.Dispose();
            session.BeatStep = 0;
            session.BeatSchedule = Scheduler.ScheduleRepeating(
                () => OnBeat(session),
                TimeSpan.FromSeconds(session.Tempo.GetBeatSeconds() / session.StepsPerBeat));
        }

        private static void OnBeat(PerformanceSession session)
        {
            var player = session.Performer;
            var profilerStart = FrameProfiler.Start();
            try
            {
                if (!_sessions.TryGetValue(player, out var live) || live != session)
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

                if (DateTime.UtcNow >= session.AnimationRenewUtc)
                    StartLoopingAnimation(session);

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
                    _sessions.Remove(player);
                    session.BeatSchedule?.Dispose();
                }
            }
            finally
            {
                FrameProfiler.Stop(FrameMetric.Scheduler, profilerStart, "PerformanceBeat");
            }
        }

        private static PerformanceEndReason? GetEndTrigger(PerformanceSession session)
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
            if (session.Kind == PerformanceKind.Music && !IsInstrumentStillEquipped(session))
                return PerformanceEndReason.InstrumentRemoved;
            if (session.IsIdle(DateTime.UtcNow))
                return PerformanceEndReason.Idle;

            return null;
        }

        // One slot lookup per beat. Sessions started under the no-instrument test allowance have no item to check.
        private static bool IsInstrumentStillEquipped(PerformanceSession session)
        {
            return session.Instrument == OBJECT_INVALID ||
                   GetItemInSlot(session.InstrumentSlot, session.Performer) == session.Instrument;
        }

        private static void ApplyTempo(PerformanceSession session, PerformanceTempo tempo)
        {
            session.Tempo = tempo;
            ScheduleBeats(session);
            PerformanceWindows.RefreshSetup(session);
            SendMessageToPC(
                session.Performer,
                $"Tempo: {tempo} ({tempo.GetBeatSeconds():0.0}s beats, x{tempo.GetXPMultiplier():0.##} XP).");
        }

        private static void TryFlourish(PerformanceSession session, DateTime now)
        {
            var performer = session.Performer;
            if (!session.TryConsumeFlourish(now))
            {
                SendMessageToPC(performer, $"Flourish is not ready ({session.FlourishMeter}/{PerformanceMath.FlourishMeterMax}).");
                return;
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Improve_Ability_Score), performer);
            PlayThenLoop(session, PerformanceAnimations.GetFlourishClip(session.Kind, session.InstrumentFamily, session.FlourishesPlayed));
            session.FlourishesPlayed++;
            SendMessageToPC(performer, "Flourish!");
            PerformanceAudience.OnFlourish(session);
        }

        // The cue window takes only what changed; with it closed, one line of screen text keeps the performance playable.
        private static void PresentBeat(PerformanceSession session, PerformanceBeatOutcome outcome)
        {
            if (session.View is { IsOpen: true } view)
            {
                view.ShowBeat(session, outcome);
                return;
            }

            ShowFallbackBeat(session);
        }

        private static void ShowFallbackBeat(PerformanceSession session)
        {
            var strip = string.Join(" | ", new[] { session.Current }.Concat(session.Upcoming)
                .Select((beat, index) => DescribeBeat(beat, index == 0)));
            PerformanceWindows.ShowFallbackStrip(
                session.Performer,
                $"{strip}  [{session.Quality}] Streak {session.Streak} Flourish {session.FlourishMeter}/{PerformanceMath.FlourishMeterMax}",
                (float)session.Tempo.GetBeatSeconds());
        }

        private static string DescribeBeat(PerformanceBeat beat, bool isCurrent)
        {
            var text = beat.IsRest
                ? "hold"
                : beat.IsChord ? $"{beat.Primary}+{beat.Second}" : beat.Primary.ToString();
            return isCurrent ? $"[{text.ToUpperInvariant()}]" : text;
        }

        private static void ApplyModeQuickbar(uint player, PerformanceKind kind, FeatType toggleFeat)
        {
            ClearFirstBar(player);

            var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
            for (var slot = 0; slot < cueFeats.Length; slot++)
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.UseFeat(cueFeats[slot]));

            PlayerPlugin.SetQuickBarSlot(player, FlourishSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceFlourish));
            PlayerPlugin.SetQuickBarSlot(player, OptionsSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceOptions));
            PlayerPlugin.SetQuickBarSlot(player, StopSlot, PlayerQuickBarSlot.UseFeat(toggleFeat));
        }

        private static void RemoveModeFeats(uint player)
        {
            foreach (var feat in _allModeFeats)
                CreaturePlugin.RemoveFeat(player, feat);
        }

        private static void ClearFirstBar(uint player)
        {
            for (var slot = 0; slot < FirstBarSlotCount; slot++)
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
        }

        private static void RestorePlayer(uint player)
        {
            RemoveModeFeats(player);

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
            {
                ClearFirstBar(player);
                return;
            }

            var saved = dbPlayer.PerformanceSavedHotBar;
            if (string.IsNullOrWhiteSpace(saved) || !CreaturePlugin.DeserializeQuickbar(player, saved))
                ClearFirstBar(player);

            dbPlayer.PerformanceSavedHotBar = null;
            DB.Set(dbPlayer);
        }

        // Restores the hot bar and feats after a crash, a failed cleanup, or a stale mode button.
        private static void RecoverInterruptedPerformance(uint player)
        {
            if (_sessions.ContainsKey(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer?.PerformanceSavedHotBar == null)
                return;

            RestorePlayer(player);
            if (Activity.GetBusyType(player) == ActivityStatusType.Performing)
                Activity.ClearBusy(player);
        }

        // The routine loop repeats in place for minutes at a time. A held weapon or horn plays through a one-shot
        // carrier instead, so there the loop clip is replayed once per cycle.
        private static void StartLoopingAnimation(PerformanceSession session)
        {
            var clip = PerformanceAnimations.GetLoopClip(session.Kind, session.InstrumentFamily, session.Routine);
            var keepHold = KeepsHold(session);
            if (!NamedAnimation.LoopsInPlace(session.Performer, keepHold))
            {
                PlayThenLoop(session, clip);
                return;
            }

            session.AnimationToken = NamedAnimation.Play(session.Performer, clip, LoopPlaybackSeconds, keepWeaponHold: keepHold);
            session.AnimationRenewUtc = DateTime.UtcNow.AddSeconds(LoopRenewSeconds);
        }

        private static bool KeepsHold(PerformanceSession session) =>
            PerformanceAnimations.KeepsInstrumentHold(session.Kind, session.InstrumentFamily);

        // A one-shot clip hands back to the routine when it ends, unless a newer clip has replaced it.
        private static void PlayThenLoop(PerformanceSession session, AnimationClip clip)
        {
            var player = session.Performer;
            var token = NamedAnimation.Play(player, clip, keepWeaponHold: KeepsHold(session));
            session.AnimationToken = token;
            session.AnimationRenewUtc = DateTime.UtcNow.AddSeconds(clip.Duration + AccentRecoverySeconds);
            AssignCommand(GetModule(), () => DelayCommand(clip.Duration, () =>
            {
                if (_sessions.TryGetValue(player, out var live) && live == session && session.AnimationToken == token)
                    StartLoopingAnimation(session);
            }));
        }

        // Walking away or attacking has already replaced the clip; only a performer standing idle bows.
        private static void ReleaseAnimation(PerformanceSession session, PerformanceEndReason reason)
        {
            var player = session.Performer;
            if (reason == PerformanceEndReason.Stopped && GetCurrentAction(player) == ActionType.Invalid)
                NamedAnimation.Play(player, AuthoredAnimation.PerformanceBow, keepWeaponHold: KeepsHold(session));
            else
                NamedAnimation.StopIfCurrent(player, session.AnimationToken);
        }

        private static void PlayHitAnimation(PerformanceSession session, PerformanceCue cue)
        {
            var clip = PerformanceAnimations.GetCueClip(session.Kind, session.InstrumentFamily, cue);
            if (clip != null)
                PlayThenLoop(session, clip);
        }

        private static string GetEndMessage(PerformanceEndReason reason)
        {
            return reason switch
            {
                PerformanceEndReason.Stopped => "You finish your performance.",
                PerformanceEndReason.Combat => "Your performance ends because you are in combat.",
                PerformanceEndReason.Moved => "Your performance ends because you moved.",
                PerformanceEndReason.Idle => "Your performance ends because you stopped following the beat.",
                PerformanceEndReason.InstrumentRemoved => "Your performance ends because your instrument was removed.",
                _ => string.Empty
            };
        }

        private static IEnumerable<FeatType> GetModeFeats(PerformanceKind kind)
        {
            var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
            return cueFeats.Concat(new[] { FeatType.PerformanceFlourish, FeatType.PerformanceOptions });
        }

        private static bool IsToggleFeat(FeatType feat)
        {
            return Array.IndexOf(_danceToggleFeats, feat) >= 0 || Array.IndexOf(_musicToggleFeats, feat) >= 0;
        }

        private static bool TryGetCue(FeatType feat, out PerformanceCue cue)
        {
            switch (feat)
            {
                case FeatType.PerformanceStepLeft: cue = PerformanceCue.Left; return true;
                case FeatType.PerformanceStepUp: cue = PerformanceCue.Up; return true;
                case FeatType.PerformanceStepDown: cue = PerformanceCue.Down; return true;
                case FeatType.PerformanceStepRight: cue = PerformanceCue.Right; return true;
                case FeatType.PerformanceNoteLow: cue = PerformanceCue.Low; return true;
                case FeatType.PerformanceNoteMid: cue = PerformanceCue.Mid; return true;
                case FeatType.PerformanceNoteHigh: cue = PerformanceCue.High; return true;
                default: cue = PerformanceCue.Left; return false;
            }
        }

        private static bool TryParseFamily(string text, out PerformanceInstrumentFamily family)
        {
            return Enum.TryParse(text, true, out family) &&
                   family != PerformanceInstrumentFamily.None &&
                   Enum.IsDefined(family);
        }
    }
}

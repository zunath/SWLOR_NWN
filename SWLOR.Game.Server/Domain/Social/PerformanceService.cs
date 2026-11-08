using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
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
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Dance and Play Instrument performances: the hot bar cues, the beat loop, the session and the end triggers. The logic
    /// that used to be static in <c>Performance</c> is moved here unchanged, and the live sessions and the players'
    /// preferences are this instance's own. The event handlers run on the container's
    /// instance; static callers reach it through <c>GameServices.Performance</c>.
    /// </summary>
    public class PerformanceService : IPerformanceService
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

        private readonly Dictionary<uint, PerformanceSession> _sessions = new();
        private readonly Dictionary<string, PerformancePreference> _preferences = new();

        private readonly FeatType[] _danceToggleFeats =
            { FeatType.Dance1, FeatType.Dance2, FeatType.Dance3, FeatType.Dance4 };

        private readonly FeatType[] _musicToggleFeats =
            { FeatType.PlayInstrument1, FeatType.PlayInstrument2, FeatType.PlayInstrument3, FeatType.PlayInstrument4 };

        private readonly FeatType[] _danceCueFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight
        };

        private readonly FeatType[] _musicCueFeats =
        {
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh
        };

        private readonly FeatType[] _allModeFeats =
        {
            FeatType.PerformanceStepLeft, FeatType.PerformanceStepUp,
            FeatType.PerformanceStepDown, FeatType.PerformanceStepRight,
            FeatType.PerformanceNoteLow, FeatType.PerformanceNoteMid, FeatType.PerformanceNoteHigh,
            FeatType.PerformanceFlourish, FeatType.PerformanceOptions
        };

        private readonly IDBService _db;
        private readonly INamedAnimationService _namedAnimation;
        private readonly IPerformanceAudienceService _audience;
        private readonly IPerformanceWindowsService _windows;
        private readonly IPerformanceSongService _songs;
        private readonly IPerformanceRiderService _riders;
        private readonly ICraftService _craft;
        private readonly IActivityService _activity;
        private readonly IPerkService _perk;

        public PerformanceService(
            IDBService db,
            INamedAnimationService namedAnimation,
            IPerformanceAudienceService audience,
            IPerformanceWindowsService windows,
            IPerformanceSongService songs,
            IPerformanceRiderService riders,
            ICraftService craft,
            IActivityService activity,
            IPerkService perk)
        {
            _db = db;
            _namedAnimation = namedAnimation;
            _audience = audience;
            _windows = windows;
            _songs = songs;
            _riders = riders;
            _craft = craft;
            _activity = activity;
            _perk = perk;
        }

        public IReadOnlyList<PerformanceSongOption> GetAvailableSongs(string playerId)
        {
            return _songs.GetAvailableSongs(playerId);
        }

        public int GetRequiredRank(PerkType perk)
        {
            return _riders.GetRequiredRank(perk);
        }

        public void ToggleDance(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Dance, level);
        }

        public void TogglePlayInstrument(uint activator, int level)
        {
            Toggle(activator, PerformanceKind.Music, level);
        }

        public bool IsModeFeat(FeatType feat)
        {
            return Array.IndexOf(_allModeFeats, feat) >= 0;
        }

        /// <summary>A performer's own Dance or Play Instrument feat doubles as the Stop button, so it is routed here before the busy check can refuse it.</summary>
        public bool IsModeFeat(uint activator, FeatType feat)
        {
            return IsModeFeat(feat) || _sessions.ContainsKey(activator) && IsToggleFeat(feat);
        }

        public bool TryGetSession(uint performer, out PerformanceSession session)
        {
            return _sessions.TryGetValue(performer, out session);
        }

        public IReadOnlyList<PerformanceSession> GetSessions()
        {
            return _sessions.Values.ToArray();
        }

        public PerformancePreference GetPreference(uint player)
        {
            var playerId = GetObjectUUID(player);
            if (!_preferences.TryGetValue(playerId, out var preference))
            {
                preference = new PerformancePreference();
                _preferences[playerId] = preference;
            }

            return preference;
        }

        public bool IsRoutineUnlocked(uint player, PerformanceRoutineType routine)
        {
            return GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(routine)) > 0;
        }

        public bool IsSetUnlocked(uint player, PerformanceSetType set)
        {
            return GetPerkLevel(player, PerformanceRiderCatalog.GetUnlockPerk(set)) > 0;
        }

        public bool SetRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!IsRoutineUnlocked(player, routine))
                return false;

            GetPreference(player).Routine = routine;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Dance)
            {
                session.Routine = routine;
                StartLoopingAnimation(session);
                _windows.RefreshSetup(session);
            }

            return true;
        }

        public bool SetSet(uint player, PerformanceSetType set)
        {
            if (!IsSetUnlocked(player, set))
                return false;

            GetPreference(player).Set = set;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                session.Set = set;
                _windows.RefreshSetup(session);
            }

            return true;
        }

        /// <summary>Picks the blended second routine. Only a dancer with Showstopper can use it.</summary>
        public bool SetSecondRoutine(uint player, PerformanceRoutineType routine)
        {
            if (!IsRoutineUnlocked(player, routine) || GetPerkLevel(player, PerkType.Showstopper) <= 0)
                return false;

            GetPreference(player).SecondRoutine = routine;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Dance)
                _windows.RefreshSetup(session);

            return true;
        }

        /// <summary>Picks the woven second set. Only a musician with Virtuoso can use it.</summary>
        public bool SetSecondSet(uint player, PerformanceSetType set)
        {
            if (!IsSetUnlocked(player, set) || GetPerkLevel(player, PerkType.Virtuoso) <= 0)
                return false;

            GetPreference(player).SecondSet = set;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
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

            GetPreference(player).SongTrackId = trackId;
            if (_sessions.TryGetValue(player, out var session) && session.Kind == PerformanceKind.Music)
            {
                _audience.SetPerformerSong(player, trackId);
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
            GetPreference(player).Roleplay = roleplay;
            if (!_sessions.TryGetValue(player, out var session) || session.IsRoleplay == roleplay)
                return;

            session.SetRoleplay(roleplay, UtcNow());
            ApplyModeQuickbar(player, session.Kind, session.ToggleFeat, roleplay);

            if (session.View is { IsOpen: true } view)
                view.ShowMode(roleplay);
            else if (!roleplay && !_windows.OpenCueWindow(session))
                ShowFallbackBeat(session);

            if (roleplay)
                _windows.ClearFallbackStrip(player);

            _windows.RefreshSetup(session);
            SendMessageToPC(player, GetModeMessage(session.Kind, roleplay));
        }

        public void SetTempo(uint player, PerformanceTempo tempo)
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
        public bool HasInstrumentEquipped(uint player, out PerformanceInstrumentFamily family)
        {
            return TryFindInstrument(player, out family, out _, out _);
        }

        // Found once when a performance starts; each beat then checks only that slot (see IsInstrumentStillEquipped).
        private bool TryFindInstrument(
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

        public void HandleInput(uint activator, FeatType feat)
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

            if (!TryGetCue(feat, out var cue) || !PerformanceCueGenerator.GetLanes(session.Kind).Contains(cue))
                return;

            if (session.RegisterCue(cue, now) == PerformanceInputResult.Hit)
                PlayHitAnimation(session, cue);
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

                RestorePlayer(player);
                ApplyHornFit(player);
                if (GetBusyType(player) == ActivityStatusType.Performing)
                    ClearBusy(player);

                if (reason != PerformanceEndReason.LoggedOut)
                {
                    if (reason != PerformanceEndReason.Died)
                        ReleaseAnimation(session, reason);

                    var message = GetEndMessage(reason, session.IsRoleplay);
                    if (!string.IsNullOrEmpty(message))
                        SendMessageToPC(player, message);
                }
            }

            try
            {
                _audience.OnSessionEnded(session, reason);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Performance audience cleanup failed: PlayerId={PlayerId}", session.PlayerId);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void RecoverOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            RecoverInterruptedPerformance(player, atLogin: true);
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

            if (!_sessions.TryGetValue(ChatPlugin.GetSender(), out var session) || !session.IsRoleplay)
                return;

            var message = ChatPlugin.GetMessage()?.Trim() ?? string.Empty;
            if (message.Length == 0 || message.StartsWith('/'))
                return;

            session.NoteActivity(UtcNow());
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void EndOnLogout()
        {
            EndPerformance(GetExitingObject(), PerformanceEndReason.LoggedOut);
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
            if (!_sessions.TryGetValue(player, out var session) || session.Kind != PerformanceKind.Music)
                return;

            if (!string.IsNullOrEmpty(GetLocalString(GetPCItemLastUnequipped(), InstrumentFamilyVariable)))
                EndPerformance(player, PerformanceEndReason.InstrumentRemoved);
        }

        private void Toggle(uint player, PerformanceKind kind, int level)
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
            else if (IsBusy(player))
                error = "You are busy.";
            else if (IsPlayerInSpaceMode(player))
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
            RecoverInterruptedPerformance(player);

            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
            {
                SendMessageToPC(player, "You cannot perform right now.");
                return;
            }

            var toggleFeats = kind == PerformanceKind.Dance ? _danceToggleFeats : _musicToggleFeats;
            var toggleFeat = toggleFeats[Math.Clamp(level, 1, toggleFeats.Length) - 1];

            dbPlayer.PerformanceSavedHotBar = CreaturePlugin.SerializeQuickbar(player) ?? string.Empty;
            dbPlayer.PerformanceHotBarRestoreSequence = 0;
            _db.Set(dbPlayer);

            foreach (var feat in GetModeFeats(kind))
                CreaturePlugin.AddFeat(player, feat);

            var preference = GetPreference(player);
            ApplyModeQuickbar(player, kind, toggleFeat, preference.Roleplay);
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
                Routine = IsRoutineUnlocked(player, preference.Routine)
                    ? preference.Routine
                    : PerformanceRoutineType.CantinaShuffle,
                Set = IsSetUnlocked(player, preference.Set)
                    ? preference.Set
                    : PerformanceSetType.CantinaStandards
            };

            session.SetRoleplay(preference.Roleplay, now);

            _sessions[player] = session;
            ApplyHornFit(player);
            _audience.OnPerformanceStarted(player);
            // The toggle has no cast window to play its clip in, so the performance opens with it.
            PlayThenLoop(
                session,
                PerformanceAnimations.GetOpeningClip(kind),
                PerformanceAnimations.GetOpeningHoldMode(kind, family, GetResRef(instrument)));
            ScheduleBeats(session);

            SendMessageToPC(player, GetStartMessage(kind, session.IsRoleplay));

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

                if (UtcNow() >= session.AnimationRenewUtc)
                    StartLoopingAnimation(session);

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
                    _sessions.Remove(player);
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
            if (session.Kind == PerformanceKind.Music && !IsInstrumentStillEquipped(session))
                return PerformanceEndReason.InstrumentRemoved;
            if (session.IsIdle(UtcNow()))
                return PerformanceEndReason.Idle;

            return null;
        }

        // One slot lookup per beat. Sessions started under the no-instrument test allowance have no item to check.
        private bool IsInstrumentStillEquipped(PerformanceSession session)
        {
            return session.Instrument == OBJECT_INVALID ||
                   GetItemInSlot(session.InstrumentSlot, session.Performer) == session.Instrument;
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
            PlayThenLoop(session, PerformanceAnimations.GetFlourishClip(session.Kind, session.InstrumentFamily, session.FlourishesPlayed));
            session.FlourishesPlayed++;
            SendMessageToPC(performer, "Flourish!");
            _audience.OnFlourish(session);
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

        // Options and Stop keep their slots in both modes; Roleplay mode leaves the cue and Flourish slots empty.
        private void ApplyModeQuickbar(uint player, PerformanceKind kind, FeatType toggleFeat, bool roleplay)
        {
            ClearFirstBar(player);

            if (!roleplay)
            {
                var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
                for (var slot = 0; slot < cueFeats.Length; slot++)
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.UseFeat(cueFeats[slot]));

                PlayerPlugin.SetQuickBarSlot(player, FlourishSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceFlourish));
            }

            PlayerPlugin.SetQuickBarSlot(player, OptionsSlot, PlayerQuickBarSlot.UseFeat(FeatType.PerformanceOptions));
            PlayerPlugin.SetQuickBarSlot(player, StopSlot, PlayerQuickBarSlot.UseFeat(toggleFeat));
        }

        private void RemoveModeFeats(uint player)
        {
            foreach (var feat in _allModeFeats)
                CreaturePlugin.RemoveFeat(player, feat);
        }

        private void ClearFirstBar(uint player)
        {
            for (var slot = 0; slot < FirstBarSlotCount; slot++)
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
        }

        private void RestorePlayer(uint player)
        {
            RemoveModeFeats(player);

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
            {
                ClearFirstBar(player);
                return;
            }

            var saved = dbPlayer.PerformanceSavedHotBar;
            if (string.IsNullOrWhiteSpace(saved) || !CreaturePlugin.DeserializeQuickbar(player, saved))
                ClearFirstBar(player);

            // An export finishes after this script, so only a later login can acknowledge the restored bar.
            dbPlayer.PerformanceHotBarRestoreSequence = AdvanceFileSequence(player);
            _db.Set(dbPlayer);
            PersistCharacter(player);
        }

        // Restores the hot bar and feats after a crash, a failed cleanup, or a stale mode button.
        private void RecoverInterruptedPerformance(uint player, bool atLogin = false)
        {
            if (_sessions.ContainsKey(player))
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var action = PerformanceHotbarRecovery.Decide(
                dbPlayer.PerformanceSavedHotBar,
                dbPlayer.PerformanceHotBarRestoreSequence,
                LoadedSequence(player),
                atLogin);
            if (action == PerformanceHotbarRecoveryAction.None)
                return;

            if (action == PerformanceHotbarRecoveryAction.Restore)
                RestorePlayer(player);
            else
            {
                dbPlayer.PerformanceSavedHotBar = null;
                dbPlayer.PerformanceHotBarRestoreSequence = 0;
                _db.Set(dbPlayer);
            }

            if (GetBusyType(player) == ActivityStatusType.Performing)
                ClearBusy(player);
        }

        // Every performance clip plays on the looping carrier, so the routine loop repeats in place for minutes at a time.
        private void StartLoopingAnimation(PerformanceSession session)
        {
            var clip = PerformanceAnimations.GetLoopClip(session.Kind, session.InstrumentFamily, session.Routine);
            session.AnimationToken = _namedAnimation.Play(session.Performer, clip, LoopPlaybackSeconds, holdMode: HoldMode(session));
            session.AnimationRenewUtc = UtcNow().AddSeconds(LoopRenewSeconds);
        }

        private NamedAnimationHold HoldMode(PerformanceSession session) =>
            PerformanceAnimations.GetHoldMode(session.Kind, session.InstrumentFamily);

        // A one-shot clip hands back to the routine when it ends, unless a newer clip has replaced it.
        private void PlayThenLoop(PerformanceSession session, AnimationClip clip) =>
            PlayThenLoop(session, clip, HoldMode(session));

        private void PlayThenLoop(PerformanceSession session, AnimationClip clip, NamedAnimationHold holdMode)
        {
            var player = session.Performer;
            var token = _namedAnimation.Play(player, clip, holdMode: holdMode);
            session.AnimationToken = token;
            session.AnimationRenewUtc = UtcNow().AddSeconds(clip.Duration + AccentRecoverySeconds);
            AssignCommand(GetModule(), () => DelayCommand(clip.Duration, () =>
            {
                if (_sessions.TryGetValue(player, out var live) && live == session && session.AnimationToken == token)
                    StartLoopingAnimation(session);
            }));
        }

        // Walking away or attacking has already replaced the clip; only a performer standing idle bows.
        private void ReleaseAnimation(PerformanceSession session, PerformanceEndReason reason)
        {
            var player = session.Performer;
            if (reason == PerformanceEndReason.Stopped && GetCurrentAction(player) == ActionType.Invalid)
                _namedAnimation.Play(player, AuthoredAnimation.PerformanceBow, holdMode: HoldMode(session));
            else
                _namedAnimation.StopIfCurrent(player, session.AnimationToken);
        }

        private void PlayHitAnimation(PerformanceSession session, PerformanceCue cue)
        {
            var clip = PerformanceAnimations.GetCueClip(session.Kind, session.InstrumentFamily, cue);
            if (clip != null)
                PlayThenLoop(session, clip);
        }

        private string GetStartMessage(PerformanceKind kind, bool roleplay)
        {
            if (roleplay)
            {
                return kind == PerformanceKind.Dance
                    ? $"You begin to dance in Roleplay mode. {RoleplayKeepGoingText}"
                    : $"You begin to play in Roleplay mode. {RoleplayKeepGoingText}";
            }

            return kind == PerformanceKind.Dance
                ? "You begin to dance. Use the step buttons on your hot bar to match the cues, or switch to Roleplay mode under Options."
                : "You begin to play. Use the note buttons on your hot bar to match the cues, or switch to Roleplay mode under Options.";
        }

        private string GetModeMessage(PerformanceKind kind, bool roleplay)
        {
            if (roleplay)
                return $"Roleplay mode: no cues, and your audience banks Inspired at the Rough rate. {RoleplayKeepGoingText}";

            return kind == PerformanceKind.Dance
                ? "Active play: use the step buttons on your hot bar to match the cues."
                : "Active play: use the note buttons on your hot bar to match the cues.";
        }

        private string RoleplayKeepGoingText =>
            $"Talk or emote at least once every {RoleplayIdleMinutes} minutes to keep performing.";

        private int RoleplayIdleMinutes => (int)(PerformanceMath.RoleplayIdleTimeoutSeconds / 60);

        private string GetEndMessage(PerformanceEndReason reason, bool roleplay)
        {
            if (reason == PerformanceEndReason.Idle && roleplay)
                return $"Your performance ends because you were quiet for {RoleplayIdleMinutes} minutes.";

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

        private IEnumerable<FeatType> GetModeFeats(PerformanceKind kind)
        {
            var cueFeats = kind == PerformanceKind.Dance ? _danceCueFeats : _musicCueFeats;
            return cueFeats.Concat(new[] { FeatType.PerformanceFlourish, FeatType.PerformanceOptions });
        }

        private bool IsToggleFeat(FeatType feat)
        {
            return Array.IndexOf(_danceToggleFeats, feat) >= 0 || Array.IndexOf(_musicToggleFeats, feat) >= 0;
        }

        private bool TryGetCue(FeatType feat, out PerformanceCue cue)
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

        private bool TryParseFamily(string text, out PerformanceInstrumentFamily family)
        {
            return Enum.TryParse(text, true, out family) &&
                   family != PerformanceInstrumentFamily.None &&
                   Enum.IsDefined(family);
        }

        // The calls into the unconverted Scheduler, Perk, Activity and Space statics and the clock, one line each so a
        // test can answer them.

        /// <summary>The time now.</summary>
        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual void ApplyHornFit(uint player) =>
            Feature.AppearanceDefinition.ItemAppearance.HornFit.Apply(player);

        protected virtual IDisposable ScheduleRepeating(Action task, TimeSpan interval) =>
            Scheduler.ScheduleRepeating(task, interval);

        protected virtual int GetPerkLevel(uint creature, PerkType perk) => _perk.GetPerkLevel(creature, perk);

        // The craft session world's character file checkpoint, behind the converted Craft service.
        protected virtual int AdvanceFileSequence(uint player) => _craft.SessionWorld.AdvanceFileSequence(player);

        protected virtual int LoadedSequence(uint player) => _craft.SessionWorld.LoadedSequence(player);

        protected virtual void PersistCharacter(uint player) => _craft.SessionWorld.PersistCharacter(player);

        protected virtual bool IsBusy(uint target) => _activity.IsBusy(target);

        protected virtual ActivityStatusType GetBusyType(uint target) => _activity.GetBusyType(target);

        protected virtual void SetBusy(uint target, ActivityStatusType type) => _activity.SetBusy(target, type);

        protected virtual void ClearBusy(uint target) => _activity.ClearBusy(target);

        protected virtual bool IsPlayerInSpaceMode(uint player) => Space.IsPlayerInSpaceMode(player);

        public virtual int ChooseSong(string playerId, int preferredTrackId = PerformanceSongs.AutomaticSong) =>
            _songs.ChooseSong(playerId, preferredTrackId);
    }
}

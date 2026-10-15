using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// DM-toggled recorder that writes every combat event in one area to a JSON Lines file plus
    /// a per-participant summary. Off by default: while no recording runs, every hook in the
    /// game code is a single <see cref="IsRecording"/> check. While recording, hooks only build
    /// small event structs; serialization and file I/O happen on <see cref="CombatRecordWriter"/>'s
    /// background thread.
    /// </summary>
    public static class CombatRecorder
    {
        private const int MaximumAttributionDepth = 32;
        // Deferred AssignCommand damage runs on the next AI update, a frame or two later.
        private const ulong DeferredDamageFrameWindow = 10;
        // Weapon damage is rolled before the attack animation lands.
        private const long WeaponDamageWindowMs = 4000;
        private const long SnapshotIntervalMs = 1000;

        private static readonly CombatRecordAttribution[] _attributions = new CombatRecordAttribution[MaximumAttributionDepth];
        private static int _attributionDepth;
        private static readonly Dictionary<uint, int> _participantIds = new();
        private static readonly List<uint> _participants = new();
        private static readonly List<CombatRecordPendingDamage> _pendingDamage = new();
        private static readonly Dictionary<(uint Target, string Key), (int SourceId, CombatRecordAttribution Attribution)> _temporaryHitPointGrants = new();
        private static readonly Dictionary<uint, uint> _lastDamageSources = new();
        private static readonly List<CombatRecordDamageApplication> _damageApplications = new();
        private static readonly List<CombatRecordEvent> _heldEvents = new();
        private static readonly List<CombatRecordWriter> _unfinishedWriters = new();
        private static readonly Dictionary<uint, ulong> _deathFrames = new();
        private static uint _toggleDeactivatedBy = OBJECT_INVALID;
        private static readonly List<CombatRecordEvent> _awaitingHeal = new();
        private static uint _awaitingHealTarget = OBJECT_INVALID;
        private static ulong _awaitingHealFrame;
        private static bool _isInHealingReaction;
        private static ulong _toggleDeactivatedFrame;
        private static int _heldInsertIndex = -1;
        private static long _heldTimestampMs;
        private static Dictionary<AbilityDetail, FeatType> _abilityFeats;

        private static CombatRecordWriter _writer;
        private static IDisposable _snapshotSchedule;
        private static uint _area;
        private static string _label;
        private static DateTime _startedUtc;
        private static int _eventsEnqueued;
        private static string _hookWarning;
        // Numbers each recording so a captured attribution can tell which one it came from.
        private static int _recording;

        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        public static bool IsRecording { get; private set; }

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        public static CombatRecordWriter LastWriter { get; private set; }

        public static uint RecordedArea => IsRecording ? _area : OBJECT_INVALID;

        /// <summary>
        /// Starts recording every creature in an area.
        /// </summary>
        /// <param name="area">The area to record.</param>
        /// <param name="label">Optional label for the file name. Defaults to the area resref.</param>
        /// <param name="message">A message describing the result for the DM.</param>
        public static bool Start(uint area, string label, out string message)
        {
            if (IsRecording)
            {
                message = $"A combat recording is already running in {GetName(_area)} ('{_label}'). Stop it first.";
                return false;
            }

            if (!GetIsObjectValid(area) || GetArea(area) != area)
            {
                message = "Combat recording needs a valid area.";
                return false;
            }

            _hookWarning = null;
            try
            {
                CombatRecorderEffectHooks.Install();
            }
            catch (Exception ex)
            {
                _hookWarning = $"Damage and healing capture is unavailable: {ex.Message}";
                Log.Write(LogGroup.Error, $"Combat recorder could not hook damage/healing: {ex}", true);
            }

            _abilityFeats ??= CombatRecordAbilityFeatIndex.Build(Ability.GetAllAbilityDetails());
            _label = string.IsNullOrWhiteSpace(label) ? GetResRef(area) : label.Trim();
            _area = area;
            _startedUtc = DateTime.UtcNow;
            _eventsEnqueued = 0;
            _recording++;
            _attributionDepth = 0;
            _participantIds.Clear();
            _participants.Clear();
            _pendingDamage.Clear();
            _temporaryHitPointGrants.Clear();
            _lastDamageSources.Clear();
            _damageApplications.Clear();
            _heldEvents.Clear();
            _heldInsertIndex = -1;
            _deathFrames.Clear();
            _awaitingHeal.Clear();
            _isInHealingReaction = false;

            var directory = CombatRecordFileNames.GetDirectory(ApplicationSettings.Get().LogDirectory);
            var baseName = CombatRecordFileNames.BuildBaseName(_startedUtc, _label);
            _writer = new CombatRecordWriter(
                directory + baseName + CombatRecordFileNames.EventsExtension,
                directory + baseName + CombatRecordFileNames.SummaryExtension);
            LastWriter = _writer;
            _unfinishedWriters.RemoveAll(writer => writer.Completion.IsCompleted);
            _unfinishedWriters.Add(_writer);
            _writer.Start();

            IsRecording = true;
            Enqueue(CombatRecordEvent.RecordingStarted(_label, GetName(area), GetResRef(area)));
            TakeSnapshots();
            _snapshotSchedule = Scheduler.ScheduleRepeating(
                TakeSnapshots,
                TimeSpan.FromMilliseconds(SnapshotIntervalMs),
                TimeSpan.FromMilliseconds(SnapshotIntervalMs));

            Log.Write(LogGroup.DM, $"Combat recording '{_label}' started in {GetName(area)} ({GetResRef(area)}): {_writer.EventsPath}");
            message = $"Combat recording '{_label}' started in {GetName(area)}. File: {_writer.EventsPath}";
            if (_hookWarning != null)
                message += $" WARNING: {_hookWarning}";
            return true;
        }

        /// <summary>
        /// Stops the current recording. The writer finishes the files in the background.
        /// </summary>
        public static bool Stop(string reason, out string message)
        {
            if (!IsRecording)
            {
                message = "No combat recording is running.";
                return false;
            }

            _damageApplications.Clear();
            _isInHealingReaction = false;
            ReleaseAwaitingHeal();
            WriteHeldEvents();
            Enqueue(CombatRecordEvent.RecordingStopped(reason, _eventsEnqueued + 1));
            IsRecording = false;
            _snapshotSchedule?.Dispose();
            _snapshotSchedule = null;
            _writer.Complete();

            var duration = DateTime.UtcNow - _startedUtc;
            Log.Write(LogGroup.DM, $"Combat recording '{_label}' stopped ({reason}) after {duration.TotalSeconds:0}s, {_eventsEnqueued} events.");
            message = $"Combat recording '{_label}' stopped after {duration.TotalSeconds:0}s with {_eventsEnqueued} events and {_participants.Count} participants. " +
                      $"Files: {_writer.EventsPath} and {_writer.SummaryPath}";

            _attributionDepth = 0;
            _participantIds.Clear();
            _participants.Clear();
            _pendingDamage.Clear();
            _temporaryHitPointGrants.Clear();
            _lastDamageSources.Clear();
            _damageApplications.Clear();
            _deathFrames.Clear();
            return true;
        }

        public static string GetStatus()
        {
            if (!IsRecording)
            {
                var last = LastWriter;
                if (last == null)
                    return "No combat recording is running.";

                var state = last.Completion.IsCompleted
                    ? last.Error == null ? "finished" : $"failed: {last.Error}"
                    : "still writing";
                return $"No combat recording is running. Last recording ({state}): {last.EventsPath}";
            }

            var elapsed = DateTime.UtcNow - _startedUtc;
            var status = $"Recording '{_label}' in {GetName(_area)} for {elapsed.TotalSeconds:0}s: " +
                         $"{_participants.Count} participants, {_eventsEnqueued} events ({_writer.EventsWritten} written). " +
                         $"File: {_writer.EventsPath}";
            if (_writer.Error != null)
                status += $" WRITER ERROR: {_writer.Error}";
            if (_hookWarning != null)
                status += $" WARNING: {_hookWarning}";
            return status;
        }

        [NWNEventHandler(ScriptName.OnSWLORApplicationShutdown)]
        public static void StopOnShutdown()
        {
            if (IsRecording)
                Stop("server shutdown", out _);

            // Recordings stopped earlier, not only the latest, may still be writing their files.
            var deadline = DateTime.UtcNow.AddSeconds(10);
            foreach (var writer in _unfinishedWriters)
            {
                var remaining = deadline - DateTime.UtcNow;
                if (!writer.Completion.IsCompleted && remaining > TimeSpan.Zero)
                    writer.WaitForExit(remaining);
            }
        }

        #region Attribution

        internal static void RestoreAttributionDepth(int depth)
        {
            if (depth >= 0 && depth < _attributionDepth)
                _attributionDepth = depth;
        }

        // The Begin* entry points are inlined so a hook costs only the IsRecording check while
        // no recording runs; the default scope they return disposes as a no-op.

        /// <summary>
        /// Attributes changes made inside the returned scope to an actor and source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAttribution(
            uint actor, CombatRecordSourceKind kind, string name = null, int code = 0, int rank = 0)
        {
            return IsRecording ? PushAttribution(actor, kind, name, code, rank) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAbilityAttribution(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid)
        {
            return IsRecording && ability != null
                ? PushAttribution(activator, CombatRecordSourceKind.Ability, ability.Name, GetFeatCode(ability, feat), ability.AbilityLevel)
                : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect)
        {
            return IsRecording ? PushStatusEffectAttribution(source, creature, statusEffect) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginItemAttribution(uint user, uint item)
        {
            return IsRecording ? PushItemAttribution(user, item) : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat)
        {
            return IsRecording ? PushAttribution(creature, CombatRecordSourceKind.StatTrigger, null, (int)stat, 0) : default;
        }

        /// <summary>
        /// Starts a rider scope (lifesteal, damage share) that keeps the enclosing ability's name.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginRiderAttribution(uint actor, CombatRecordSourceKind kind)
        {
            return IsRecording ? PushRiderAttribution(actor, kind) : default;
        }

        /// <summary>
        /// Attributes a delayed effect to the ability that scheduled it, named by its feat. Unlike
        /// a captured scope this also covers pulses scheduled before the recording started.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAbilityFeatAttribution(uint activator, FeatType feat)
        {
            return IsRecording && feat != FeatType.Invalid ? PushAbilityFeatAttribution(activator, feat) : default;
        }

        /// <summary>
        /// Attributes a trap going off to whatever placed it (the ability or kit captured with
        /// <see cref="CaptureScope"/>), or to a trap of its owner when it was placed before this
        /// recording started, including during an earlier recording.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginTrapAttribution(uint owner, in CombatRecordAttribution placedBy)
        {
            if (!IsRecording)
                return default;

            return placedBy.Actor != 0 && placedBy.Recording == _recording
                ? BeginAttribution(placedBy)
                : PushAttribution(owner, CombatRecordSourceKind.Trap, null, 0, 0);
        }

        /// <summary>
        /// Marks a companion's setup heal (its deploy health correction), which is not combat
        /// healing and is not recorded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginSetupAttribution(uint creature)
        {
            return IsRecording ? PushAttribution(creature, CombatRecordSourceKind.Setup, null, 0, 0) : default;
        }

        /// <summary>
        /// Starts the reactions to a heal on <paramref name="creature"/> that is applied right after
        /// them (a stamina restore on healing received). Their events are written after that heal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordHealingReactionScope BeginHealingReaction(uint creature)
        {
            return IsRecording ? StartHealingReaction(creature) : default;
        }

        private static CombatRecordHealingReactionScope StartHealingReaction(uint creature)
        {
            ReleaseAwaitingHeal();
            _awaitingHealTarget = creature;
            _awaitingHealFrame = ServerFrame.Current;
            _isInHealingReaction = true;
            return new CombatRecordHealingReactionScope(true);
        }

        internal static void EndHealingReaction()
        {
            _isInHealingReaction = false;
        }

        /// <summary>
        /// Writes reaction events still waiting for their heal, in case it was never applied.
        /// </summary>
        private static void ReleaseAwaitingHeal()
        {
            if (_awaitingHeal.Count == 0)
                return;

            var awaiting = _awaitingHeal.ToArray();
            _awaitingHeal.Clear();
            foreach (var recordEvent in awaiting)
                Enqueue(recordEvent);
        }

        /// <summary>
        /// Attributes a respawn's own recovery, so it is not counted as anyone's healing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginRespawnAttribution()
        {
            return IsRecording ? PushAttribution(OBJECT_INVALID, CombatRecordSourceKind.Respawn, null, 0, 0) : default;
        }

        /// <summary>
        /// The innermost open scope, or an inactive value when none is open, so queued work can
        /// restore exactly that scope with <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttribution CaptureScope()
        {
            if (!IsRecording || _attributionDepth <= 0)
                return default;

            var attribution = _attributions[_attributionDepth - 1];
            attribution.Recording = _recording;
            return attribution;
        }

        /// <summary>
        /// Attribution for damage split off a hit and applied later: the attacker's own open scope
        /// (the ability that hit), or a weapon swing when no scope is open.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttribution CaptureAttackAttribution(uint attacker)
        {
            if (!IsRecording)
                return default;

            var attribution = ResolveAttackAttribution(attacker);
            attribution.Recording = _recording;
            return attribution;
        }

        private static CombatRecordAttribution ResolveAttackAttribution(uint attacker)
        {
            if (_attributionDepth > 0 && IsOwnedBy(_attributions[_attributionDepth - 1], attacker))
                return _attributions[_attributionDepth - 1];

            return new CombatRecordAttribution(
                attacker, CombatRecordSourceKind.Autoattack, null, (int)Combat.GetEquippedWeaponSkillType(attacker), 0);
        }

        /// <summary>
        /// Restores a captured attribution. One captured during an earlier recording is ignored, since
        /// its source belongs to that recording.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAttribution(in CombatRecordAttribution attribution)
        {
            return IsRecording && attribution.Actor != 0 && attribution.Recording == _recording
                ? PushAttribution(attribution.Actor, attribution.Kind, attribution.Name, attribution.Code, attribution.Rank, attribution.Owner)
                : default;
        }

        private static CombatRecordAttributionScope PushAttribution(
            uint actor, CombatRecordSourceKind kind, string name, int code, int rank, uint owner = 0)
        {
            var restoreDepth = _attributionDepth;
            if (_attributionDepth < MaximumAttributionDepth)
            {
                _attributions[_attributionDepth] = new CombatRecordAttribution(actor, kind, name, code, rank, owner);
                _attributionDepth++;
            }

            return new CombatRecordAttributionScope(restoreDepth);
        }

        private static CombatRecordAttributionScope PushStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect)
        {
            var actor = GetIsObjectValid(source) ? source : creature;
            return PushAttribution(actor, CombatRecordSourceKind.StatusEffect, statusEffect.Name, 0, 0, creature);
        }

        private static CombatRecordAttributionScope PushAbilityFeatAttribution(uint activator, FeatType feat)
        {
            if (!Ability.IsFeatRegistered(feat))
                return default;

            var ability = Ability.GetAbilityDetail(feat);
            return PushAttribution(activator, CombatRecordSourceKind.Ability, ability.Name, (int)feat, ability.AbilityLevel);
        }

        private static CombatRecordAttributionScope PushItemAttribution(uint user, uint item)
        {
            return PushAttribution(user, CombatRecordSourceKind.Item, GetName(item), 0, 0);
        }

        private static CombatRecordAttributionScope PushRiderAttribution(uint actor, CombatRecordSourceKind kind)
        {
            var current = ResolveAttribution(actor);
            var isAbility = current.Kind == CombatRecordSourceKind.Ability;
            return PushAttribution(actor, kind, isAbility ? current.Name : null, isAbility ? current.Code : 0, current.Rank);
        }

        /// <summary>
        /// Resolves who caused a change: the innermost attribution scope, then the actor's active
        /// ability impact, then the actor alone.
        /// </summary>
        private static CombatRecordAttribution ResolveAttribution(uint fallbackActor)
        {
            if (_attributionDepth > 0)
            {
                var attribution = _attributions[_attributionDepth - 1];
                if (!GetIsObjectValid(attribution.Actor))
                    attribution.Actor = fallbackActor;
                return attribution;
            }

            return ResolveActiveAbilityAttribution(fallbackActor);
        }

        /// <summary>
        /// The innermost scope when it belongs to <paramref name="actor"/>; otherwise the actor's
        /// active ability impact, then the actor alone. Used where another creature's scope
        /// must not lend its source to the actor.
        /// </summary>
        private static CombatRecordAttribution ResolveOwnAttribution(uint actor)
        {
            if (!GetIsObjectValid(actor))
                return ResolveAttribution(actor);
            if (_attributionDepth > 0 && IsOwnedBy(_attributions[_attributionDepth - 1], actor))
                return _attributions[_attributionDepth - 1];

            return ResolveActiveAbilityAttribution(actor);
        }

        private static bool IsOwnedBy(in CombatRecordAttribution scope, uint actor)
        {
            return scope.Actor == actor || scope.Owner == actor;
        }

        private static CombatRecordAttribution ResolveActiveAbilityAttribution(uint actor)
        {
            var ability = GetIsObjectValid(actor)
                ? Ability.GetActiveAbilityImpactSummary(actor)?.Ability
                : null;
            return ability != null
                ? new CombatRecordAttribution(actor, CombatRecordSourceKind.Ability, ability.Name, GetFeatCode(ability, FeatType.Invalid), ability.AbilityLevel)
                : new CombatRecordAttribution(actor, CombatRecordSourceKind.Unattributed, null, 0, 0);
        }

        /// <summary>
        /// The activated feat when known, else the feat the ability is registered under.
        /// </summary>
        private static int GetFeatCode(AbilityDetail ability, FeatType feat)
        {
            if (feat != FeatType.Invalid)
                return (int)feat;

            return _abilityFeats != null && ability != null && _abilityFeats.TryGetValue(ability, out var registeredFeat)
                ? (int)registeredFeat
                : (int)FeatType.Invalid;
        }

        #endregion

        #region Participants

        /// <summary>
        /// Events raised inside an engine damage call (reactions, death scripts) are held until
        /// that call's damage event is in place ahead of them.
        /// </summary>
        private static void Enqueue(CombatRecordEvent recordEvent)
        {
            if (_isInHealingReaction)
            {
                recordEvent.Frame = ServerFrame.Current;
                recordEvent.TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                _awaitingHeal.Add(recordEvent);
                return;
            }

            // A heal that never came: its reactions are written once that frame has passed.
            if (_awaitingHeal.Count > 0 && _awaitingHealFrame != ServerFrame.Current)
                ReleaseAwaitingHeal();

            recordEvent.Frame = ServerFrame.Current;
            if (_heldInsertIndex >= 0)
            {
                recordEvent.TimestampMs = _heldTimestampMs;
                _heldEvents.Insert(_heldInsertIndex++, recordEvent);
                return;
            }

            recordEvent.TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (_damageApplications.Count > 0)
                _heldEvents.Add(recordEvent);
            else
                Write(recordEvent);
        }

        private static void Write(in CombatRecordEvent recordEvent)
        {
            _eventsEnqueued++;
            _writer.Enqueue(recordEvent);
        }

        private static void WriteHeldEvents()
        {
            _heldInsertIndex = -1;
            if (IsRecording)
            {
                foreach (var recordEvent in _heldEvents)
                    Write(recordEvent);
            }

            _heldEvents.Clear();
        }

        private static bool IsInRecordedArea(uint creature)
        {
            return creature != OBJECT_INVALID && GetIsObjectValid(creature) && GetArea(creature) == _area;
        }

        /// <summary>
        /// Events are kept when either side is in the recorded area.
        /// </summary>
        private static bool IsRecorded(uint subject, uint other = OBJECT_INVALID)
        {
            return IsInRecordedArea(subject) || IsInRecordedArea(other);
        }

        /// <summary>
        /// Returns the creature's stable id for this recording, registering it on first sight.
        /// Returns 0 for anything that is not a creature.
        /// </summary>
        private static int GetParticipantId(uint creature)
        {
            if (creature == OBJECT_INVALID || creature == 0)
                return 0;

            if (_participantIds.TryGetValue(creature, out var participantId))
                return participantId;

            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return 0;

            participantId = _participantIds.Count + 1;
            _participantIds[creature] = participantId;
            _participants.Add(creature);

            // Character names only; never account names, CD keys or IP addresses. A declaration is
            // written at once, never held, so it precedes every event that refers to it.
            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            var declaration = CombatRecordEvent.Participant(
                participantId,
                GetName(creature),
                isPlayer,
                isPlayer ? null : GetResRef(creature));
            declaration.TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            declaration.Frame = ServerFrame.Current;
            Write(declaration);
            return participantId;
        }

        private static void RegisterPlayersInArea()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) != _area || DMPresence.IsControlledByDM(player))
                    continue;

                GetParticipantId(player);
            }
        }

        private static void TakeSnapshots()
        {
            if (!IsRecording)
                return;

            if (!GetIsObjectValid(_area))
            {
                Stop("recorded area no longer exists", out _);
                return;
            }

            RegisterPlayersInArea();
            for (var index = 0; index < _participants.Count; index++)
            {
                var creature = _participants[index];
                if (!IsInRecordedArea(creature))
                    continue;

                Player dbPlayer = null;
                if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
                {
                    dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
                    if (dbPlayer == null)
                        continue;
                }

                var temporaryHitPoints = TemporaryHitPointEffects.GetRemaining(creature);
                Enqueue(CombatRecordEvent.Snapshot(
                    _participantIds[creature],
                    GetCurrentHitPoints(creature) - temporaryHitPoints,
                    GetMaxHitPoints(creature),
                    temporaryHitPoints,
                    Stat.GetCurrentFP(creature, dbPlayer),
                    Stat.GetMaxFP(creature, dbPlayer),
                    Stat.GetCurrentStamina(creature, dbPlayer),
                    Stat.GetMaxStamina(creature, dbPlayer),
                    GetIsInCombat(creature),
                    GetIsDead(creature)));
            }
        }

        #endregion

        #region Abilities

        public static void RecordAbilityCast(
            uint activator, uint target, Location targetLocation, AbilityDetail ability, FeatType feat, float activationDelay,
            bool isToggleOff = false)
        {
            if (ability == null || !IsRecorded(activator, target))
                return;

            var recordEvent = CombatRecordEvent.AbilityCast(
                GetParticipantId(activator),
                GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                (int)ability.ActivationType,
                (int)Math.Round(activationDelay * 1000f),
                isToggleOff);
            Enqueue(WithTargetLocation(recordEvent, target, targetLocation));
        }

        /// <summary>
        /// Records an attempted activation the ability's rules refused, with the refusal shown.
        /// </summary>
        /// <param name="atCompletion">True when the cast was refused as it completed.</param>
        public static void RecordAbilityDenied(
            uint activator, uint target, AbilityDetail ability, FeatType feat, string reason, bool atCompletion)
        {
            if (ability == null || !IsRecorded(activator, target))
                return;

            Enqueue(CombatRecordEvent.AbilityDenied(
                GetParticipantId(activator),
                GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                reason,
                atCompletion));
        }

        /// <summary>
        /// Records a cast or channel stopped before it completed. A damage interruption names the
        /// creature that dealt the damage; an <see cref="CombatRecordInterruptCause.Other"/> stop made
        /// inside another creature's scope is that creature's crowd control.
        /// </summary>
        public static void RecordAbilityInterrupted(
            uint activator, AbilityDetail ability, CombatRecordInterruptCause cause, uint damagedBy = OBJECT_INVALID)
        {
            if (ability == null || !IsInRecordedArea(activator))
                return;

            var interrupter = default(CombatRecordAttribution);
            if (cause == CombatRecordInterruptCause.Damage)
            {
                if (GetIsObjectValid(damagedBy) && damagedBy != activator)
                    interrupter = ResolveOwnAttribution(damagedBy);
            }
            else if (cause == CombatRecordInterruptCause.Other &&
                     _attributionDepth > 0 &&
                     _attributions[_attributionDepth - 1].Actor != activator)
            {
                interrupter = _attributions[_attributionDepth - 1];
                cause = CombatRecordInterruptCause.CrowdControl;
            }

            Enqueue(CombatRecordEvent.AbilityInterrupted(
                GetParticipantId(activator),
                ability.Name,
                GetFeatCode(ability, FeatType.Invalid),
                ability.AbilityLevel,
                cause,
                GetParticipantId(interrupter.Actor),
                interrupter.Kind,
                interrupter.Name,
                interrupter.Code));
        }

        public static void RecordAbilityQueued(uint activator, uint target, AbilityDetail ability, FeatType feat)
        {
            if (ability == null || !IsRecorded(activator, target))
                return;

            Enqueue(CombatRecordEvent.AbilityQueued(
                GetParticipantId(activator),
                GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel));
        }

        public static void RecordAbilityImpact(
            uint activator, uint target, Location targetLocation, AbilityDetail ability, FeatType feat, AbilityImpactSummary summary)
        {
            if (ability == null || (!IsRecorded(activator, target) && !AnyInRecordedArea(summary?.ImpactedTargets)))
                return;

            var recordEvent = CombatRecordEvent.AbilityImpact(
                GetParticipantId(activator),
                GetParticipantId(target),
                ability.Name,
                (int)feat,
                ability.AbilityLevel,
                summary?.ImpactedTargetCount ?? 0,
                summary?.CriticalHitCount ?? 0,
                summary?.AttributedDamage ?? 0);
            Enqueue(WithTargetLocation(recordEvent, target, targetLocation));
        }

        /// <summary>
        /// Notes that an activation turned a toggled stance off, which is a use of the ability
        /// even though its activation action reports no impact.
        /// </summary>
        public static void NoteToggleDeactivated(uint activator)
        {
            _toggleDeactivatedBy = activator;
            _toggleDeactivatedFrame = ServerFrame.Current;
        }

        /// <summary>
        /// Whether the activation that just ran turned <paramref name="activator"/>'s stance off.
        /// </summary>
        public static bool TakeToggleDeactivation(uint activator)
        {
            var deactivated = _toggleDeactivatedBy == activator && _toggleDeactivatedFrame == ServerFrame.Current;
            _toggleDeactivatedBy = OBJECT_INVALID;
            return deactivated;
        }

        /// <summary>
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        public static void RecordRecurringAbilityImpact(uint activator, AbilityDetail ability, AbilityImpactSummary summary)
        {
            // A fixed-location pulse keeps landing in the area after its caster leaves.
            if (ability == null || (!IsInRecordedArea(activator) && !AnyInRecordedArea(summary?.ImpactedTargets)))
                return;

            Enqueue(CombatRecordEvent.AbilityImpact(
                GetParticipantId(activator),
                0,
                ability.Name,
                GetFeatCode(ability, FeatType.Invalid),
                ability.AbilityLevel,
                summary?.ImpactedTargetCount ?? 0,
                summary?.CriticalHitCount ?? 0,
                summary?.AttributedDamage ?? 0,
                isRecurring: true));
        }

        private static bool AnyInRecordedArea(IReadOnlyCollection<uint> creatures)
        {
            if (creatures == null)
                return false;

            foreach (var creature in creatures)
            {
                if (IsInRecordedArea(creature))
                    return true;
            }

            return false;
        }

        private static CombatRecordEvent WithTargetLocation(CombatRecordEvent recordEvent, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target) || !GetIsObjectValid(GetAreaFromLocation(targetLocation)))
                return recordEvent;

            var position = GetPositionFromLocation(targetLocation);
            return recordEvent.WithLocation(position.X, position.Y, position.Z);
        }

        /// <summary>
        /// Wraps an ability's activation requirements: measures the FP and STM they take and
        /// attributes resource changes inside the scope (including consumed items) to the
        /// ability's cost. Hit points an ability spends are self-damage during its impact and
        /// are recorded as damage attributed to the cost.
        /// </summary>
        public static CombatRecordAbilityCostScope BeginAbilityCost(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid)
        {
            if (!IsRecording || ability == null || !IsInRecordedArea(activator))
                return default;

            var attribution = BeginAttribution(activator, CombatRecordSourceKind.AbilityCost, ability.Name, (int)feat, ability.AbilityLevel);
            return new CombatRecordAbilityCostScope(
                activator,
                ability,
                (int)feat,
                Stat.GetCurrentFP(activator),
                Stat.GetCurrentStamina(activator),
                attribution);
        }

        internal static void CompleteAbilityCost(in CombatRecordAbilityCostScope cost)
        {
            cost.Attribution.Dispose();
            if (!IsRecording || !GetIsObjectValid(cost.Activator))
                return;

            Enqueue(CombatRecordEvent.AbilityCost(
                GetParticipantId(cost.Activator),
                cost.Ability.Name,
                cost.Feat,
                cost.Ability.AbilityLevel,
                cost.FPBefore - Stat.GetCurrentFP(cost.Activator),
                cost.StaminaBefore - Stat.GetCurrentStamina(cost.Activator)));
        }

        /// <param name="attackResult">The native attack result: 1/7 hit, 2 deflect, 3 critical, 4 miss.</param>
        public static void RecordAttack(uint attacker, uint defender, int attackResult, int hitChance, string abilityName)
        {
            if (!IsRecorded(defender, attacker))
                return;

            Enqueue(CombatRecordEvent.Attack(
                GetParticipantId(attacker),
                GetParticipantId(defender),
                ToAttackResult(attackResult),
                hitChance,
                abilityName));
        }

        private static CombatRecordAttackResult ToAttackResult(int attackResult)
        {
            return attackResult switch
            {
                1 or 7 => CombatRecordAttackResult.Hit,
                2 => CombatRecordAttackResult.Deflect,
                3 => CombatRecordAttackResult.Critical,
                4 => CombatRecordAttackResult.Miss,
                _ => CombatRecordAttackResult.Unknown
            };
        }

        #endregion

        #region Damage

        /// <summary>
        /// Registers damage that a deferred AssignCommand will apply, so the native damage hook
        /// can attribute it to the scope that queued it (a damage-over-time tick, a damage share).
        /// Direct damage normally lands inside its ability's scope or through the weapon hook;
        /// it is registered only when a status effect computes it, because status ticks and
        /// reactions (Burn, Poison and others) dispatch Direct damage through AssignCommand too.
        /// </summary>
        public static void ExpectDeferredDamage(uint source, uint target, int amount, CombatDamageDeliveryType deliveryType)
        {
            // A status that outlived its source dispatches its damage from the creature it lands on.
            if (!GetIsObjectValid(source))
                source = target;

            if (amount <= 0 || !IsRecorded(target, source))
                return;

            // Direct damage is registered only where it is dispatched later: a status effect's tick
            // or reaction, or the source's own scope (a trap going off, an item) with no ability
            // impact to queue the hit into.
            if (deliveryType == CombatDamageDeliveryType.Direct)
            {
                if (_attributionDepth == 0)
                    return;

                var scope = _attributions[_attributionDepth - 1];
                var isDispatchedLater = scope.Kind == CombatRecordSourceKind.StatusEffect ||
                                        (IsOwnedBy(scope, source) && Ability.GetActiveAbilityImpactSummary(source) == null);
                if (!isDispatchedLater)
                    return;
            }

            // Triggered and damage-over-time hits computed during the source's own ability impact
            // are queued with that impact's hits and land under the scope they were queued in.
            if ((deliveryType == CombatDamageDeliveryType.Triggered || deliveryType == CombatDamageDeliveryType.DamageOverTime) &&
                Ability.GetActiveAbilityImpactSummary(source) != null)
            {
                return;
            }

            // A reflection or reaction the defender deals during the attacker's ability must not
            // inherit that ability.
            var attribution = ResolveOwnAttribution(source);
            if (deliveryType == CombatDamageDeliveryType.Transferred &&
                attribution.Kind != CombatRecordSourceKind.StatusEffect)
            {
                attribution.Kind = CombatRecordSourceKind.DamageShare;
            }

            AddPendingDamage(source, target, amount, false, attribution);
        }

        /// <summary>
        /// Whether a registration is waiting for damage from <paramref name="source"/> to
        /// <paramref name="target"/>. Observability seam for the engine tests.
        /// </summary>
        public static bool HasPendingDamage(uint source, uint target)
        {
            return _pendingDamage.Exists(pending => pending.Source == source && pending.Target == target);
        }

        /// <summary>
        /// Registers hit points an ability spends by damaging its own caster.
        /// </summary>
        public static void ExpectHitPointCost(uint activator, int amount)
        {
            if (amount <= 0 || !IsInRecordedArea(activator))
                return;

            var attribution = ResolveAttribution(activator);
            attribution.Kind = CombatRecordSourceKind.AbilityCost;
            AddPendingDamage(activator, activator, amount, false, attribution);
        }

        /// <summary>
        /// Registers a landed weapon swing's rolled damage, applied when the attack animation lands.
        /// </summary>
        public static void ExpectWeaponDamage(uint attacker, uint defender, int amount, SkillType weaponSkill)
        {
            if (amount <= 0 || !IsRecorded(defender, attacker))
                return;

            var attribution = new CombatRecordAttribution(attacker, CombatRecordSourceKind.Autoattack, null, (int)weaponSkill, 0);
            AddPendingDamage(attacker, defender, amount, true, attribution);
        }

        private static void AddPendingDamage(uint source, uint target, int amount, bool isWeaponHit, CombatRecordAttribution attribution)
        {
            _pendingDamage.Add(new CombatRecordPendingDamage
            {
                Source = source,
                Target = target,
                Amount = amount,
                IsWeaponHit = isWeaponHit,
                Frame = ServerFrame.Current,
                TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Attribution = attribution
            });
        }

        /// <summary>
        /// Attribution for damage the engine is applying right now: a matching synchronous scope,
        /// then a pending deferred or weapon registration (exact amount first), then the
        /// creator's active ability impact.
        /// </summary>
        private static CombatRecordAttribution ResolveDamageAttribution(uint source, uint target, int amount, bool isEngineApplied)
        {
            if (_attributionDepth > 0)
            {
                var scope = _attributions[_attributionDepth - 1];
                if (IsOwnedBy(scope, source))
                    return scope;
            }

            var match = CombatRecordPendingDamageMatcher.FindMatch(
                _pendingDamage,
                source,
                target,
                amount,
                isEngineApplied,
                ServerFrame.Current,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                DeferredDamageFrameWindow,
                WeaponDamageWindowMs);
            if (match >= 0)
            {
                var attribution = _pendingDamage[match].Attribution;
                _pendingDamage.RemoveAt(match);
                return attribution;
            }

            return ResolveActiveAbilityAttribution(source);
        }

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <see cref="RecordDamageApplied"/> and <see cref="EndDamageApplication"/>.
        /// </summary>
        internal static int BeginDamageApplication(uint source, uint target, int amount, int hitPointsBefore)
        {
            // Only damage the recording keeps (it deals damage and involves the area) can name a
            // death's last damage source.
            if (amount > 0 && source != target && source != 0 && source != OBJECT_INVALID && IsRecorded(target, source))
                _lastDamageSources[target] = source;

            // A reaction hitting the same creature again ends what the outer hit left.
            var outer = FindDamageApplication(target);
            if (outer >= 0 && !_damageApplications[outer].HasHitPointsLeftByHit)
                KeepHitPointsLeftByHit(outer, hitPointsBefore);

            _damageApplications.Add(new CombatRecordDamageApplication
            {
                Target = target,
                HeldEventStart = _heldEvents.Count,
                StartedMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            return _damageApplications.Count - 1;
        }

        /// <summary>
        /// Releases a damage application, and writes the held events once the outermost one ends.
        /// </summary>
        internal static void EndDamageApplication(int applicationIndex)
        {
            if (applicationIndex >= 0 && applicationIndex < _damageApplications.Count)
                _damageApplications.RemoveRange(applicationIndex, _damageApplications.Count - applicationIndex);

            if (_damageApplications.Count == 0 && _heldEvents.Count > 0)
                WriteHeldEvents();
        }

        /// <summary>
        /// Called by the native damage hook after the engine applied a damage effect.
        /// Hit points exclude temporary HP, which is reported separately.
        /// </summary>
        /// <param name="isEngineApplied">True when no script or closure applied the effect, as for weapon hits.</param>
        internal static void RecordDamageApplied(
            int applicationIndex,
            bool isEngineApplied,
            uint source,
            uint target,
            in CombatRecordDamageBreakdown damage,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            CombatRecordTemporaryHitPointPool[] poolsBefore,
            CombatRecordTemporaryHitPointPool[] poolsAfter)
        {
            try
            {
                if (applicationIndex < 0 || applicationIndex >= _damageApplications.Count || !IsRecording)
                    return;

                // A subdued player is resurrected inside the lethal damage call; report the HP the
                // hit left, captured when the death scripts ran.
                var application = _damageApplications[applicationIndex];
                if (application.HasHitPointsLeftByHit)
                    hitPointsAfter = application.HitPointsLeftByHit;

                if (IsRecorded(target, source))
                {
                    // The damage event goes ahead of everything the hit triggered.
                    _heldInsertIndex = application.HeldEventStart;
                    _heldTimestampMs = application.StartedMs;
                    var poolsLeftByHit = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(poolsAfter, application.PoolsAtRemoval);
                    RecordDamageEvents(isEngineApplied, source, target, damage, hitPointsBefore, hitPointsAfter, maxHitPoints,
                        poolsBefore, poolsAfter, poolsLeftByHit);
                }
            }
            finally
            {
                _heldInsertIndex = -1;
                EndDamageApplication(applicationIndex);
            }
        }

        private static void RecordDamageEvents(
            bool isEngineApplied,
            uint source,
            uint target,
            in CombatRecordDamageBreakdown damage,
            int hitPointsBefore,
            int hitPointsAfter,
            int maxHitPoints,
            CombatRecordTemporaryHitPointPool[] poolsBefore,
            CombatRecordTemporaryHitPointPool[] poolsAfter,
            CombatRecordTemporaryHitPointPool[] poolsLeftByHit)
        {
            var absorbedTotal = CombatRecordTemporaryHitPointPools.SumAbsorbed(poolsBefore, poolsLeftByHit);

            // A swing whose damage was carved away entirely applies an empty damage effect.
            if (damage.Total <= 0 && hitPointsAfter == hitPointsBefore && absorbedTotal == 0)
                return;

            var attribution = ResolveDamageAttribution(source, target, damage.Total, isEngineApplied);
            var sourceId = GetParticipantId(source);
            var targetId = GetParticipantId(target);
            var temporaryBefore = CombatRecordTemporaryHitPointPools.Sum(poolsBefore);
            var temporaryAfter = CombatRecordTemporaryHitPointPools.Sum(poolsAfter);

            Enqueue(CombatRecordEvent.DamageApplied(
                sourceId,
                targetId,
                attribution.Kind,
                attribution.Name,
                attribution.Code,
                damage,
                hitPointsBefore,
                hitPointsAfter,
                temporaryBefore,
                temporaryAfter,
                absorbedTotal,
                maxHitPoints,
                isEngineApplied));

            if (poolsBefore != null)
            {
                foreach (var pool in poolsBefore)
                {
                    var absorbed = CombatRecordTemporaryHitPointPools.GetAbsorbed(pool, poolsLeftByHit);
                    if (absorbed <= 0)
                        continue;

                    var hasGrant = _temporaryHitPointGrants.TryGetValue((target, pool.EffectKey), out var grant);
                    Enqueue(CombatRecordEvent.TemporaryHitPointsAbsorbed(
                        sourceId,
                        targetId,
                        pool.EffectKey,
                        absorbed,
                        hasGrant ? grant.SourceId : 0,
                        hasGrant ? grant.Attribution.Kind : CombatRecordSourceKind.Unattributed,
                        hasGrant ? grant.Attribution.Name : null,
                        hasGrant ? grant.Attribution.Code : 0));
                }
            }
        }

        #endregion

        #region Healing and temporary HP

        /// <summary>
        /// Called by the native heal hook after the engine applied a heal effect.
        /// </summary>
        internal static void RecordHealingApplied(uint creator, uint target, int requested, int hitPointsBefore, int hitPointsAfter, int maxHitPoints)
        {
            // A heal reacting to a hit still in progress ends what that hit left.
            var application = FindDamageApplication(target);
            if (application >= 0 && !_damageApplications[application].HasHitPointsLeftByHit)
                KeepHitPointsLeftByHit(application, hitPointsBefore);

            if (!IsRecorded(target, creator))
            {
                ReleaseAwaitingHealFor(target);
                return;
            }

            var attribution = ResolveAttribution(creator);
            if (attribution.Kind == CombatRecordSourceKind.Setup)
                return;

            Enqueue(CombatRecordEvent.Healing(
                GetParticipantId(attribution.Actor),
                GetParticipantId(target),
                attribution.Kind,
                attribution.Name,
                attribution.Code,
                requested,
                hitPointsBefore,
                hitPointsAfter,
                maxHitPoints));

            ReleaseAwaitingHealFor(target);
        }

        private static void ReleaseAwaitingHealFor(uint target)
        {
            if (_awaitingHeal.Count > 0 && _awaitingHealTarget == target)
                ReleaseAwaitingHeal();
        }

        /// <summary>
        /// Called before a keyed temporary-HP pool is removed or replaced. During a hit on that
        /// creature, what each pool had left is kept, so the hit's absorption is measured against
        /// that remainder instead of treating the removed pool as fully absorbed.
        /// </summary>
        public static void RecordTemporaryHitPointPoolRemoving(uint creature)
        {
            var index = FindDamageApplication(creature);
            if (index < 0)
                return;

            var application = _damageApplications[index];
            application.PoolsAtRemoval = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(
                application.PoolsAtRemoval,
                CombatRecorderEffectHooks.CaptureTemporaryHitPointPools(creature));
            _damageApplications[index] = application;
        }

        /// <summary>
        /// Records a pool cut back to fit the temporary HP cap, credited to whoever granted it.
        /// </summary>
        public static void RecordTemporaryHitPointsTrimmed(uint target, string effectKey, int trimmed, int remaining)
        {
            if (trimmed <= 0 || !IsInRecordedArea(target))
                return;

            var hasGrant = _temporaryHitPointGrants.TryGetValue((target, effectKey), out var grant);
            Enqueue(CombatRecordEvent.TemporaryHitPointsTrimmed(
                GetParticipantId(target),
                effectKey,
                trimmed,
                remaining,
                hasGrant ? grant.SourceId : 0,
                hasGrant ? grant.Attribution.Kind : CombatRecordSourceKind.Unattributed,
                hasGrant ? grant.Attribution.Name : null,
                hasGrant ? grant.Attribution.Code : 0));
        }

        public static void RecordTemporaryHitPointsGranted(uint source, uint target, string effectKey, int amount, float durationSeconds)
        {
            if (!IsRecorded(target, source))
            {
                // The new pool replaced any recorded pool with this key; its absorbs are not that granter's.
                _temporaryHitPointGrants.Remove((target, effectKey));
                return;
            }

            // An explicit granter must not inherit another creature's scope, such as the
            // attacker's ability during a defender's reactive shield.
            var attribution = GetIsObjectValid(source) ? ResolveOwnAttribution(source) : ResolveAttribution(target);

            var sourceId = GetParticipantId(attribution.Actor);
            _temporaryHitPointGrants[(target, effectKey)] = (sourceId, attribution);
            Enqueue(CombatRecordEvent.TemporaryHitPointsGranted(
                sourceId,
                GetParticipantId(target),
                attribution.Kind,
                attribution.Name,
                attribution.Code,
                effectKey,
                amount,
                (int)Math.Round(durationSeconds * 1000f)));
        }

        #endregion

        #region Resources

        /// <summary>
        /// Records an FP or STM change that already happened.
        /// </summary>
        public static void RecordResourceChange(uint creature, CombatRecordResource resource, int delta, int valueAfter)
        {
            if (delta == 0 || !IsInRecordedArea(creature))
                return;

            var attribution = ResolveAttribution(creature);
            Enqueue(CombatRecordEvent.ResourceChanged(
                GetParticipantId(creature), resource, delta, valueAfter,
                attribution.Kind, attribution.Name, attribution.Code));
        }

        /// <summary>
        /// Records an FP or STM reduction that is about to clamp the pool at zero.
        /// </summary>
        public static void RecordResourceReduction(uint creature, CombatRecordResource resource, int reduceBy, int currentValue)
        {
            var reduction = Math.Min(Math.Max(0, reduceBy), Math.Max(0, currentValue));
            RecordResourceChange(creature, resource, -reduction, currentValue - reduction);
        }

        /// <summary>
        /// Records a stack reduction made by combat activity (an item use, an ability's cost).
        /// Reductions outside any scope, such as quest turn-ins, are not combat events.
        /// </summary>
        public static void RecordItemStackReduced(uint item, int quantity)
        {
            if (_attributionDepth == 0)
                return;

            RecordItemConsumed(GetItemPossessor(item), GetResRef(item), quantity);
        }

        /// <summary>
        /// Records a charge an activated item spent, attributed to that item.
        /// </summary>
        public static void RecordItemChargeUsed(uint user, uint item)
        {
            if (!IsInRecordedArea(user))
                return;

            using var attribution = PushItemAttribution(user, item);
            RecordItemConsumed(user, GetResRef(item), 1);
        }

        public static void RecordItemConsumed(uint creature, string itemResref, int quantity)
        {
            if (quantity <= 0 || !IsInRecordedArea(creature))
                return;

            var attribution = ResolveAttribution(creature);
            Enqueue(CombatRecordEvent.ResourceChanged(
                GetParticipantId(creature), CombatRecordResource.Item, -quantity, -1,
                attribution.Kind, attribution.Name, attribution.Code, itemResref));
        }

        #endregion

        #region Status effects

        public static void RecordStatusApplied(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds, bool isPermanent, bool isRefresh)
        {
            if (statusEffect == null || !IsRecorded(creature, source))
                return;

            Enqueue(CombatRecordEvent.StatusApplied(
                GetParticipantId(source),
                GetParticipantId(creature),
                statusEffect.Name,
                statusEffect.GetType().Name,
                statusEffect.OriginatingAbility?.Name ?? FindAbilityScopeName(source),
                isPermanent ? CombatRecordEvent.PermanentDuration : (int)Math.Round(durationSeconds * 1000f),
                isRefresh));
        }

        /// <summary>
        /// The ability whose scope is applying something for <paramref name="actor"/>, such as a
        /// delayed pulse that runs after its impact ended.
        /// </summary>
        private static string FindAbilityScopeName(uint actor)
        {
            for (var depth = _attributionDepth - 1; depth >= 0; depth--)
            {
                var scope = _attributions[depth];
                if (scope.Kind == CombatRecordSourceKind.Ability && scope.Actor == actor)
                    return scope.Name;
            }

            return null;
        }

        public static void RecordStatusRemoved(uint creature, IStatusEffect statusEffect, bool isReplacement)
        {
            if (statusEffect == null || !IsRecorded(creature, statusEffect.Source))
                return;

            var reason = isReplacement
                ? CombatRecordStatusRemoval.Replaced
                : statusEffect.WasNaturallyExpired
                    ? CombatRecordStatusRemoval.Expired
                    : CombatRecordStatusRemoval.Removed;
            Enqueue(CombatRecordEvent.StatusRemoved(
                GetParticipantId(statusEffect.Source),
                GetParticipantId(creature),
                statusEffect.Name,
                statusEffect.GetType().Name,
                reason));
        }

        #endregion

        #region Deaths and resurrections

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void RecordCreatureDeath()
        {
            if (!IsRecording)
                return;

            RecordDeath(OBJECT_SELF, GetLastKiller());
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void RecordPlayerDeath()
        {
            if (!IsRecording)
                return;

            var player = GetLastPlayerDied();
            RecordDeath(player, GetLastHostileActor(player));
        }

        /// <summary>
        /// Records a respawn. Called by the respawn handler after the resurrection and before it
        /// sends the player home, which may be outside the recorded area.
        /// </summary>
        public static void RecordRespawn(uint player)
        {
            if (!IsInRecordedArea(player))
                return;

            _deathFrames.Remove(player);
            Enqueue(CombatRecordEvent.Resurrection(
                GetParticipantId(player), 0, CombatRecordSourceKind.Respawn, null, 0));
        }

        private static void RecordDeath(uint creature, uint killer)
        {
            if (!IsInRecordedArea(creature))
            {
                // A death outside the area still ends the life its last damage source belongs to.
                _lastDamageSources.Remove(creature);
                return;
            }

            // A subdual handler that ran first may already have recorded this death.
            if (IsDeathRecorded(creature))
                return;

            // Dying inside an engine damage call: keep the HP the hit left for its damage event.
            // The death event itself is held behind that damage event.
            var index = FindDamageApplication(creature);
            if (index >= 0)
            {
                var application = _damageApplications[index];
                application.DeathRecorded = true;
                _damageApplications[index] = application;
                if (!application.HasHitPointsLeftByHit)
                    KeepHitPointsLeftByHit(index, creature);
            }
            else
            {
                _deathFrames[creature] = ServerFrame.Current;
            }

            EnqueueDeath(creature, killer);
        }

        /// <summary>
        /// Whether this death was already recorded: by its lethal hit's damage application, or,
        /// outside one, in this frame since the creature's last resurrection.
        /// </summary>
        private static bool IsDeathRecorded(uint creature)
        {
            var index = FindDamageApplication(creature);
            if (index >= 0)
                return _damageApplications[index].DeathRecorded;

            return _deathFrames.TryGetValue(creature, out var deathFrame) && deathFrame == ServerFrame.Current;
        }

        /// <summary>
        /// Keeps the HP a lethal hit left when a handler inside the damage call restores the
        /// creature (a fatal-damage save), so the damage event reports the hit, not the recovery.
        /// </summary>
        public static void KeepLethalHitPoints(uint creature)
        {
            var index = FindDamageApplication(creature);
            if (index >= 0 && !_damageApplications[index].HasHitPointsLeftByHit)
                KeepHitPointsLeftByHit(index, creature);
        }

        private static void KeepHitPointsLeftByHit(int applicationIndex, uint creature)
        {
            KeepHitPointsLeftByHit(applicationIndex, CombatRecorderEffectHooks.GetHitPointsExcludingTemporary(creature));
        }

        private static void KeepHitPointsLeftByHit(int applicationIndex, int hitPoints)
        {
            var application = _damageApplications[applicationIndex];
            application.HasHitPointsLeftByHit = true;
            application.HitPointsLeftByHit = hitPoints;
            _damageApplications[applicationIndex] = application;
        }

        private static int FindDamageApplication(uint target)
        {
            for (var index = _damageApplications.Count - 1; index >= 0; index--)
            {
                if (_damageApplications[index].Target == target)
                    return index;
            }

            return -1;
        }

        private static void EnqueueDeath(uint creature, uint killer)
        {
            // A resurrected creature starts a new life with no damage source.
            _lastDamageSources.Remove(creature, out var lastDamageSource);
            Enqueue(CombatRecordEvent.Death(
                GetParticipantId(creature),
                GetParticipantId(killer),
                GetParticipantId(lastDamageSource)));
        }

        /// <summary>
        /// Records a resurrection, attributed to the innermost scope (for example the reviving
        /// ability's impact) or to <paramref name="kindWithoutScope"/>.
        /// </summary>
        public static void RecordResurrection(uint target, CombatRecordSourceKind kindWithoutScope = CombatRecordSourceKind.Unattributed)
        {
            if (!IsInRecordedArea(target))
                return;

            // A new life: a later death in this frame is a real one.
            _deathFrames.Remove(target);
            var attribution = _attributionDepth > 0
                ? _attributions[_attributionDepth - 1]
                : new CombatRecordAttribution(OBJECT_INVALID, kindWithoutScope, null, 0, 0);
            Enqueue(CombatRecordEvent.Resurrection(
                GetParticipantId(target),
                GetParticipantId(attribution.Actor),
                attribution.Kind,
                attribution.Name,
                attribution.Code));
        }

        /// <summary>
        /// Records a subdual and its resurrection. Call before the subdued player is resurrected:
        /// inside the lethal damage call both events wait for that damage and death, and the HP
        /// the hit left is kept for the damage event.
        /// </summary>
        public static void RecordSubdual(uint subdued, uint subduedBy)
        {
            if (!IsInRecordedArea(subdued))
                return;

            // The death handlers run in no fixed order, so the death is recorded first either way.
            if (!IsDeathRecorded(subdued))
                RecordDeath(subdued, GetLastHostileActor(subdued));

            var index = FindDamageApplication(subdued);
            if (index >= 0)
                KeepHitPointsLeftByHit(index, subdued);

            EnqueueSubdual(subdued, subduedBy);
        }

        private static void EnqueueSubdual(uint subdued, uint subduedBy)
        {
            var subduedId = GetParticipantId(subdued);
            Enqueue(CombatRecordEvent.Subdual(subduedId, GetParticipantId(subduedBy)));
            Enqueue(CombatRecordEvent.Resurrection(subduedId, 0, CombatRecordSourceKind.Subdual, null, 0));
        }

        #endregion
    }
}

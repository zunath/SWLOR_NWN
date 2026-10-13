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

        private static CombatRecordWriter _writer;
        private static IDisposable _snapshotSchedule;
        private static uint _area;
        private static string _label;
        private static DateTime _startedUtc;
        private static int _eventsEnqueued;
        private static string _hookWarning;

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

            _label = string.IsNullOrWhiteSpace(label) ? GetResRef(area) : label.Trim();
            _area = area;
            _startedUtc = DateTime.UtcNow;
            _eventsEnqueued = 0;
            _attributionDepth = 0;
            _participantIds.Clear();
            _participants.Clear();
            _pendingDamage.Clear();
            _temporaryHitPointGrants.Clear();
            _lastDamageSources.Clear();
            _damageApplications.Clear();

            var directory = CombatRecordFileNames.GetDirectory(ApplicationSettings.Get().LogDirectory);
            var baseName = CombatRecordFileNames.BuildBaseName(_startedUtc, _label);
            _writer = new CombatRecordWriter(
                directory + baseName + CombatRecordFileNames.EventsExtension,
                directory + baseName + CombatRecordFileNames.SummaryExtension);
            LastWriter = _writer;
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
            if (!IsRecording)
                return;

            Stop("server shutdown", out _);
            LastWriter?.WaitForExit(TimeSpan.FromSeconds(10));
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
                ? PushAttribution(activator, CombatRecordSourceKind.Ability, ability.Name, (int)feat, ability.AbilityLevel)
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
        /// Captures the current attribution so a delayed command can restore it with
        /// <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        public static CombatRecordAttribution CaptureAttribution(uint actor)
        {
            return IsRecording ? ResolveAttribution(actor) : default;
        }

        /// <summary>
        /// The innermost open scope, or an inactive value when none is open, so queued work can
        /// restore exactly that scope with <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttribution CaptureScope()
        {
            return IsRecording && _attributionDepth > 0 ? _attributions[_attributionDepth - 1] : default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CombatRecordAttributionScope BeginAttribution(in CombatRecordAttribution attribution)
        {
            return IsRecording && attribution.Actor != 0
                ? PushAttribution(attribution.Actor, attribution.Kind, attribution.Name, attribution.Code, attribution.Rank)
                : default;
        }

        private static CombatRecordAttributionScope PushAttribution(
            uint actor, CombatRecordSourceKind kind, string name, int code, int rank)
        {
            var restoreDepth = _attributionDepth;
            if (_attributionDepth < MaximumAttributionDepth)
            {
                _attributions[_attributionDepth] = new CombatRecordAttribution(actor, kind, name, code, rank);
                _attributionDepth++;
            }

            return new CombatRecordAttributionScope(restoreDepth);
        }

        private static CombatRecordAttributionScope PushStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect)
        {
            var actor = GetIsObjectValid(source) ? source : creature;
            return PushAttribution(actor, CombatRecordSourceKind.StatusEffect, statusEffect.Name, 0, 0);
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

            var ability = GetActiveAbility(fallbackActor);
            return ability != null
                ? new CombatRecordAttribution(fallbackActor, CombatRecordSourceKind.Ability, ability.Name, 0, ability.AbilityLevel)
                : new CombatRecordAttribution(fallbackActor, CombatRecordSourceKind.Unattributed, null, 0, 0);
        }

        private static AbilityDetail GetActiveAbility(uint actor)
        {
            return GetIsObjectValid(actor)
                ? Ability.GetActiveAbilityImpactSummary(actor)?.Ability
                : null;
        }

        #endregion

        #region Participants

        private static void Enqueue(CombatRecordEvent recordEvent)
        {
            recordEvent.TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            recordEvent.Frame = ServerFrame.Current;
            _eventsEnqueued++;
            _writer.Enqueue(recordEvent);
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

            // Character names only; never account names, CD keys or IP addresses.
            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            Enqueue(CombatRecordEvent.Participant(
                participantId,
                GetName(creature),
                isPlayer,
                isPlayer ? null : GetResRef(creature)));
            return participantId;
        }

        private static void RegisterPlayersInArea()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetIsDM(player) || GetIsDMPossessed(player) || GetArea(player) != _area)
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
            uint activator, uint target, Location targetLocation, AbilityDetail ability, FeatType feat, float activationDelay)
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
                (int)Math.Round(activationDelay * 1000f));
            Enqueue(WithTargetLocation(recordEvent, target, targetLocation));
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
            if (ability == null || !IsRecorded(activator, target))
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
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        public static void RecordRecurringAbilityImpact(uint activator, AbilityDetail ability, AbilityImpactSummary summary)
        {
            if (ability == null || !IsInRecordedArea(activator))
                return;

            Enqueue(CombatRecordEvent.AbilityImpact(
                GetParticipantId(activator),
                0,
                ability.Name,
                0,
                ability.AbilityLevel,
                summary?.ImpactedTargetCount ?? 0,
                summary?.CriticalHitCount ?? 0,
                summary?.AttributedDamage ?? 0,
                isRecurring: true));
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
            if (amount <= 0 || !IsRecorded(target, source))
                return;

            if (deliveryType == CombatDamageDeliveryType.Direct &&
                (_attributionDepth == 0 || _attributions[_attributionDepth - 1].Kind != CombatRecordSourceKind.StatusEffect))
            {
                return;
            }

            var attribution = ResolveAttribution(source);
            if (deliveryType == CombatDamageDeliveryType.Transferred &&
                attribution.Kind != CombatRecordSourceKind.StatusEffect)
            {
                attribution.Kind = CombatRecordSourceKind.DamageShare;
            }

            AddPendingDamage(source, target, amount, false, attribution);
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
                if (scope.Actor == source)
                {
                    // Damage registered under this scope and then applied inside it must not be
                    // left behind for a later hit to consume.
                    var registered = CombatRecordPendingDamageMatcher.FindScopeRegistration(_pendingDamage, source, target, amount, scope);
                    if (registered >= 0)
                        _pendingDamage.RemoveAt(registered);
                    return scope;
                }
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

            var ability = GetActiveAbility(source);
            return ability != null
                ? new CombatRecordAttribution(source, CombatRecordSourceKind.Ability, ability.Name, 0, ability.AbilityLevel)
                : new CombatRecordAttribution(source, CombatRecordSourceKind.Unattributed, null, 0, 0);
        }

        /// <summary>
        /// Called by the native damage hook before the engine applies a damage effect. Lethal
        /// damage runs the death scripts inside the engine call, so the source is known to them
        /// and their death event waits for this damage event. Returns the application's index
        /// for <see cref="RecordDamageApplied"/> and <see cref="EndDamageApplication"/>.
        /// </summary>
        internal static int BeginDamageApplication(uint source, uint target)
        {
            // Only damage the recording keeps can name a death's last damage source.
            if (source != target && source != 0 && source != OBJECT_INVALID && IsRecorded(target, source))
                _lastDamageSources[target] = source;

            _damageApplications.Add(new CombatRecordDamageApplication { Target = target });
            return _damageApplications.Count - 1;
        }

        /// <summary>
        /// Releases a damage application if <see cref="RecordDamageApplied"/> could not run.
        /// </summary>
        internal static void EndDamageApplication(int applicationIndex)
        {
            if (applicationIndex >= 0 && applicationIndex < _damageApplications.Count)
                _damageApplications.RemoveRange(applicationIndex, _damageApplications.Count - applicationIndex);
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
                // A subdued player is resurrected inside the lethal damage call; report the HP the
                // hit left, captured when the death scripts ran.
                if (applicationIndex >= 0 && applicationIndex < _damageApplications.Count &&
                    _damageApplications[applicationIndex].HasHitPointsAtDeath)
                {
                    hitPointsAfter = _damageApplications[applicationIndex].HitPointsAtDeath;
                }

                if (IsRecorded(target, source))
                    RecordDamageEvents(isEngineApplied, source, target, damage, hitPointsBefore, hitPointsAfter, maxHitPoints, poolsBefore, poolsAfter);
            }
            finally
            {
                CompleteDamageApplication(applicationIndex);
            }
        }

        private static void CompleteDamageApplication(int applicationIndex)
        {
            if (applicationIndex < 0 || applicationIndex >= _damageApplications.Count)
                return;

            var application = _damageApplications[applicationIndex];
            EndDamageApplication(applicationIndex);
            if (!IsRecording)
                return;

            if (application.TargetDied)
                EnqueueDeath(application.Target, application.Killer);
            if (application.TargetSubdued)
                EnqueueSubdual(application.Target, application.SubduedBy);
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
            CombatRecordTemporaryHitPointPool[] poolsAfter)
        {
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
                CombatRecordTemporaryHitPointPools.SumAbsorbed(poolsBefore, poolsAfter),
                maxHitPoints,
                isEngineApplied));

            if (poolsBefore != null)
            {
                foreach (var pool in poolsBefore)
                {
                    var absorbed = CombatRecordTemporaryHitPointPools.GetAbsorbed(pool, poolsAfter);
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
                        hasGrant ? grant.Attribution.Name : null));
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
            if (!IsRecorded(target, creator))
                return;

            var attribution = ResolveAttribution(creator);
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
        }

        public static void RecordTemporaryHitPointsGranted(uint source, uint target, string effectKey, int amount, float durationSeconds)
        {
            if (!IsRecorded(target, source))
                return;

            var attribution = ResolveAttribution(GetIsObjectValid(source) ? source : target);
            if (GetIsObjectValid(source))
                attribution.Actor = source;

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
                statusEffect.OriginatingAbility?.Name,
                isPermanent ? CombatRecordEvent.PermanentDuration : (int)Math.Round(durationSeconds * 1000f),
                isRefresh));
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

        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void RecordPlayerRespawn()
        {
            if (!IsRecording)
                return;

            var player = GetLastRespawnButtonPresser();
            if (!IsInRecordedArea(player))
                return;

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

            // Dying inside an engine damage call: record the death after that damage event.
            var index = FindDamageApplication(creature);
            if (index >= 0)
            {
                var application = _damageApplications[index];
                application.TargetDied = true;
                application.Killer = killer;
                if (!application.HasHitPointsAtDeath)
                {
                    application.HasHitPointsAtDeath = true;
                    application.HitPointsAtDeath = CombatRecorderEffectHooks.GetHitPointsExcludingTemporary(creature);
                }

                _damageApplications[index] = application;
                return;
            }

            EnqueueDeath(creature, killer);
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

            var index = FindDamageApplication(subdued);
            if (index >= 0)
            {
                var application = _damageApplications[index];
                application.TargetSubdued = true;
                application.SubduedBy = subduedBy;
                application.HasHitPointsAtDeath = true;
                application.HitPointsAtDeath = CombatRecorderEffectHooks.GetHitPointsExcludingTemporary(subdued);
                _damageApplications[index] = application;
                return;
            }

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

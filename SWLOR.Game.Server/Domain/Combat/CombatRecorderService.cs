using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// DM-toggled recorder that writes every combat event in one area to a JSON Lines file plus
    /// a per-participant summary. Off by default: while no recording runs, every hook in the
    /// game code is a single <see cref="IsRecording"/> check. While recording, hooks only build
    /// small event structs; serialization and file I/O happen on <see cref="CombatRecordWriter"/>'s
    /// background thread.
    /// </summary>
    public class CombatRecorderService : ICombatRecorderService
    {
        private const int MaximumAttributionDepth = 32;
        // Deferred AssignCommand damage runs on the next AI update, a frame or two later.
        private const ulong DeferredDamageFrameWindow = 10;
        // Weapon damage is rolled before the attack animation lands.
        private const long WeaponDamageWindowMs = 4000;
        private const long SnapshotIntervalMs = 1000;

        private readonly CombatRecordAttribution[] _attributions = new CombatRecordAttribution[MaximumAttributionDepth];
        private int _attributionDepth;
        private readonly Dictionary<(uint Target, string Key), (int SourceId, CombatRecordAttribution Attribution)> _temporaryHitPointGrants = new();
        private readonly List<CombatRecordWriter> _unfinishedWriters = new();
        private readonly Dictionary<uint, ulong> _deathFrames = new();
        private Dictionary<AbilityDetail, FeatType> _abilityFeats;

        private IDisposable _snapshotSchedule;
        private string _label;
        private DateTime _startedUtc;
        private string _hookWarning;
        // Numbers each recording so a captured attribution can tell which one it came from.
        private int _recording;

        private readonly ICombatRecordSession _session;
        private readonly IDMPresenceService _dmPresence;
        private readonly IDBService _db;
        private readonly IAbilityCatalog _catalog;
        private readonly IAbilityImpactState _impactState;
        private readonly Lazy<IStatService> _stat;
        private readonly Lazy<ICombatQueriesService> _queries;

        public CombatRecorderService(ICombatRecordSession session, IDMPresenceService dmPresence, IDBService db, IAbilityCatalog catalog, IAbilityImpactState impactState, Lazy<IStatService> stat, Lazy<ICombatQueriesService> queries)
        {
            _session = session;
            _dmPresence = dmPresence;
            _db = db;
            _catalog = catalog;
            _impactState = impactState;
            _stat = stat;
            _queries = queries;
        }

        /// <summary>
        /// True while a recording runs. Hooks check this before doing any work.
        /// </summary>
        public bool IsRecording => _session.IsRecording;

        /// <summary>
        /// The writer of the current or most recent recording. Its Completion task finishes once
        /// the summary has been written.
        /// </summary>
        public CombatRecordWriter LastWriter => _session.LastWriter;

        public uint RecordedArea => IsRecording ? _session.Area : OBJECT_INVALID;

        /// <summary>
        /// Starts recording every creature in an area.
        /// </summary>
        /// <param name="area">The area to record.</param>
        /// <param name="label">Optional label for the file name. Defaults to the area resref.</param>
        /// <param name="message">A message describing the result for the DM.</param>
        public bool Start(uint area, string label, out string message)
        {
            if (IsRecording)
            {
                message = $"A combat recording is already running in {GetName(_session.Area)} ('{_label}'). Stop it first.";
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
                InstallEffectHooks();
            }
            catch (Exception ex)
            {
                _hookWarning = $"Damage and healing capture is unavailable: {ex.Message}";
                _session.WriteLog(LogGroup.Error, $"Combat recorder could not hook damage/healing: {ex}", true);
            }

            _abilityFeats ??= CombatRecordAbilityFeatIndex.Build(GetAllAbilityDetails());
            _label = string.IsNullOrWhiteSpace(label) ? GetResRef(area) : label.Trim();
            _session.Area = area;
            _startedUtc = _session.UtcNow();
            _session.EventsEnqueued = 0;
            _recording++;
            _attributionDepth = 0;
            _session.Reset();
            _temporaryHitPointGrants.Clear();
            _deathFrames.Clear();

            var directory = CombatRecordFileNames.GetDirectory(GetLogDirectory());
            var baseName = CombatRecordFileNames.BuildBaseName(_startedUtc, _label);
            _session.Writer = new CombatRecordWriter(
                directory + baseName + CombatRecordFileNames.EventsExtension,
                directory + baseName + CombatRecordFileNames.SummaryExtension);
            _session.LastWriter = _session.Writer;
            _unfinishedWriters.RemoveAll(writer => writer.Completion.IsCompleted);
            _unfinishedWriters.Add(_session.Writer);
            _session.Writer.Start();

            _session.IsRecording = true;
            _session.Enqueue(CombatRecordEvent.RecordingStarted(_label, GetName(area), GetResRef(area)));
            TakeSnapshots();
            _snapshotSchedule = ScheduleRepeating(
                TakeSnapshots,
                TimeSpan.FromMilliseconds(SnapshotIntervalMs),
                TimeSpan.FromMilliseconds(SnapshotIntervalMs));

            _session.WriteLog(LogGroup.DM, $"Combat recording '{_label}' started in {GetName(area)} ({GetResRef(area)}): {_session.Writer.EventsPath}");
            message = $"Combat recording '{_label}' started in {GetName(area)}. File: {_session.Writer.EventsPath}";
            if (_hookWarning != null)
                message += $" WARNING: {_hookWarning}";
            return true;
        }

        /// <summary>
        /// Stops the current recording. The writer finishes the files in the background.
        /// </summary>
        public bool Stop(string reason, out string message)
        {
            if (!IsRecording)
            {
                message = "No combat recording is running.";
                return false;
            }

            _session.Flush();
            _session.Enqueue(CombatRecordEvent.RecordingStopped(reason, _session.EventsEnqueued + 1));
            _session.IsRecording = false;
            _snapshotSchedule?.Dispose();
            _snapshotSchedule = null;
            _session.Writer.Complete();

            var duration = _session.UtcNow() - _startedUtc;
            _session.WriteLog(LogGroup.DM, $"Combat recording '{_label}' stopped ({reason}) after {duration.TotalSeconds:0}s, {_session.EventsEnqueued} events.");
            message = $"Combat recording '{_label}' stopped after {duration.TotalSeconds:0}s with {_session.EventsEnqueued} events and {_session.Participants.Count} participants. " +
                      $"Files: {_session.Writer.EventsPath} and {_session.Writer.SummaryPath}";

            _attributionDepth = 0;
            _session.Reset();
            _temporaryHitPointGrants.Clear();
            _deathFrames.Clear();
            return true;
        }

        public string GetStatus()
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

            var elapsed = _session.UtcNow() - _startedUtc;
            var status = $"Recording '{_label}' in {GetName(_session.Area)} for {elapsed.TotalSeconds:0}s: " +
                         $"{_session.Participants.Count} participants, {_session.EventsEnqueued} events ({_session.Writer.EventsWritten} written). " +
                         $"File: {_session.Writer.EventsPath}";
            if (_session.Writer.Error != null)
                status += $" WRITER ERROR: {_session.Writer.Error}";
            if (_hookWarning != null)
                status += $" WARNING: {_hookWarning}";
            return status;
        }

        [BootPhase(BootReload.Retire)]
        [NWNEventHandler(ScriptName.OnSWLORApplicationShutdown)]
        public void StopOnShutdown()
        {
            if (IsRecording)
                Stop("server shutdown", out _);

            // Recordings stopped earlier, not only the latest, may still be writing their files.
            var deadline = _session.UtcNow().AddSeconds(10);
            foreach (var writer in _unfinishedWriters)
            {
                var remaining = deadline - _session.UtcNow();
                if (!writer.Completion.IsCompleted && remaining > TimeSpan.Zero)
                    writer.WaitForExit(remaining);
            }
        }

        private void RegisterPlayersInArea()
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) != _session.Area || _dmPresence.IsControlledByDM(player))
                    continue;

                _session.GetParticipantId(player);
            }
        }

        private void TakeSnapshots()
        {
            if (!IsRecording)
                return;

            if (!GetIsObjectValid(_session.Area))
            {
                Stop("recorded area no longer exists", out _);
                return;
            }

            RegisterPlayersInArea();
            for (var index = 0; index < _session.Participants.Count; index++)
            {
                var creature = _session.Participants[index];
                if (!_session.IsInRecordedArea(creature))
                    continue;

                Player dbPlayer = null;
                if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
                {
                    dbPlayer = _db.Get<Player>(GetObjectUUID(creature));
                    if (dbPlayer == null)
                        continue;
                }

                var temporaryHitPoints = GetTemporaryHitPoints(creature);
                _session.Enqueue(CombatRecordEvent.Snapshot(
                    _session.ParticipantIds[creature],
                    GetCurrentHitPoints(creature) - temporaryHitPoints,
                    GetMaxHitPoints(creature),
                    temporaryHitPoints,
                    GetCurrentFP(creature, dbPlayer),
                    GetMaxFP(creature, dbPlayer),
                    GetCurrentStamina(creature, dbPlayer),
                    GetMaxStamina(creature, dbPlayer),
                    GetIsInCombat(creature),
                    GetIsDead(creature)));
            }
        }

        #region Attribution

        public void RestoreAttributionDepth(int depth)
        {
            if (depth >= 0 && depth < _attributionDepth)
                _attributionDepth = depth;
        }

        // The Begin* entry points are inlined so a hook costs only the IsRecording check while
        // no recording runs; the default scope they return disposes as a no-op.

        /// <summary>
        /// Attributes changes made inside the returned scope to an actor and source.
        /// </summary>
        public CombatRecordAttributionScope BeginAttribution(
            uint actor, CombatRecordSourceKind kind, string name = null, int code = 0, int rank = 0)
        {
            return IsRecording ? PushAttribution(actor, kind, name, code, rank) : default;
        }

        public CombatRecordAttributionScope BeginAbilityAttribution(uint activator, AbilityDetail ability, FeatType feat = FeatType.Invalid)
        {
            return IsRecording && ability != null
                ? PushAttribution(activator, CombatRecordSourceKind.Ability, ability.Name, GetFeatCode(ability, feat), ability.AbilityLevel)
                : default;
        }

        public CombatRecordAttributionScope BeginStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect)
        {
            return IsRecording ? PushStatusEffectAttribution(source, creature, statusEffect) : default;
        }

        public CombatRecordAttributionScope BeginItemAttribution(uint user, uint item)
        {
            return IsRecording ? PushItemAttribution(user, item) : default;
        }

        public CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat)
        {
            return IsRecording ? PushAttribution(creature, CombatRecordSourceKind.StatTrigger, null, (int)stat, 0) : default;
        }

        /// <summary>
        /// Starts a rider scope (lifesteal, damage share) that keeps the enclosing ability's name.
        /// </summary>
        public CombatRecordAttributionScope BeginRiderAttribution(uint actor, CombatRecordSourceKind kind)
        {
            return IsRecording ? PushRiderAttribution(actor, kind) : default;
        }

        /// <summary>
        /// Attributes a delayed effect to the ability that scheduled it, named by its feat. Unlike
        /// a captured scope this also covers pulses scheduled before the recording started.
        /// </summary>
        public CombatRecordAttributionScope BeginAbilityFeatAttribution(uint activator, FeatType feat)
        {
            return IsRecording && feat != FeatType.Invalid ? PushAbilityFeatAttribution(activator, feat) : default;
        }

        /// <summary>
        /// Attributes a trap going off to whatever placed it (the ability or kit captured with
        /// <see cref="CaptureScope"/>), or to a trap of its owner when it was placed before this
        /// recording started, including during an earlier recording.
        /// </summary>
        public CombatRecordAttributionScope BeginTrapAttribution(uint owner, in CombatRecordAttribution placedBy)
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
        public CombatRecordAttributionScope BeginSetupAttribution(uint creature)
        {
            return IsRecording ? PushAttribution(creature, CombatRecordSourceKind.Setup, null, 0, 0) : default;
        }

        /// <summary>
        /// Starts the reactions to a heal on <paramref name="creature"/> that is applied right after
        /// them (a stamina restore on healing received). Their events are written after that heal.
        /// </summary>
        public CombatRecordHealingReactionScope BeginHealingReaction(uint creature)
        {
            return IsRecording ? StartHealingReaction(creature) : default;
        }

        private CombatRecordHealingReactionScope StartHealingReaction(uint creature)
        {
            _session.ReleaseAwaitingHeal();
            _session.AwaitingHealTarget = creature;
            _session.AwaitingHealFrame = _session.CurrentFrame();
            _session.IsInHealingReaction = true;
            return new CombatRecordHealingReactionScope(true);
        }

        public void EndHealingReaction()
        {
            _session.IsInHealingReaction = false;
        }

        /// <summary>
        /// Attributes a respawn's own recovery, so it is not counted as anyone's healing.
        /// </summary>
        public CombatRecordAttributionScope BeginRespawnAttribution()
        {
            return IsRecording ? PushAttribution(OBJECT_INVALID, CombatRecordSourceKind.Respawn, null, 0, 0) : default;
        }

        /// <summary>
        /// The innermost open scope, or an inactive value when none is open, so queued work can
        /// restore exactly that scope with <see cref="BeginAttribution(in CombatRecordAttribution)"/>.
        /// </summary>
        public CombatRecordAttribution CaptureScope()
        {
            if (!IsRecording || _attributionDepth <= 0)
                return default;

            var attribution = _attributions[_attributionDepth - 1];
            attribution.Recording = _recording;
            return attribution;
        }

        /// <summary>
        /// The innermost open scope as it is, with no recording number set, or false when none is open.
        /// </summary>
        public bool TryGetInnermostAttribution(out CombatRecordAttribution attribution)
        {
            if (_attributionDepth <= 0)
            {
                attribution = default;
                return false;
            }

            attribution = _attributions[_attributionDepth - 1];
            return true;
        }

        /// <summary>
        /// Attribution for damage split off a hit and applied later: the attacker's own open scope
        /// (the ability that hit), or a weapon swing when no scope is open.
        /// </summary>
        public CombatRecordAttribution CaptureAttackAttribution(uint attacker)
        {
            if (!IsRecording)
                return default;

            var attribution = ResolveAttackAttribution(attacker);
            attribution.Recording = _recording;
            return attribution;
        }

        private CombatRecordAttribution ResolveAttackAttribution(uint attacker)
        {
            if (_attributionDepth > 0 && IsOwnedBy(_attributions[_attributionDepth - 1], attacker))
                return _attributions[_attributionDepth - 1];

            return new CombatRecordAttribution(
                attacker, CombatRecordSourceKind.Autoattack, null, (int)GetEquippedWeaponSkillType(attacker), 0);
        }

        /// <summary>
        /// Restores a captured attribution. One captured during an earlier recording is ignored, since
        /// its source belongs to that recording.
        /// </summary>
        public CombatRecordAttributionScope BeginAttribution(in CombatRecordAttribution attribution)
        {
            return IsRecording && attribution.Actor != 0 && attribution.Recording == _recording
                ? PushAttribution(attribution.Actor, attribution.Kind, attribution.Name, attribution.Code, attribution.Rank, attribution.Owner)
                : default;
        }

        private CombatRecordAttributionScope PushAttribution(
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

        private CombatRecordAttributionScope PushStatusEffectAttribution(uint source, uint creature, IStatusEffect statusEffect)
        {
            var actor = GetIsObjectValid(source) ? source : creature;
            return PushAttribution(actor, CombatRecordSourceKind.StatusEffect, statusEffect.Name, 0, 0, creature);
        }

        private CombatRecordAttributionScope PushAbilityFeatAttribution(uint activator, FeatType feat)
        {
            if (!IsFeatRegistered(feat))
                return default;

            var ability = GetAbilityDetail(feat);
            return PushAttribution(activator, CombatRecordSourceKind.Ability, ability.Name, (int)feat, ability.AbilityLevel);
        }

        private CombatRecordAttributionScope PushItemAttribution(uint user, uint item)
        {
            return PushAttribution(user, CombatRecordSourceKind.Item, GetName(item), 0, 0);
        }

        private CombatRecordAttributionScope PushRiderAttribution(uint actor, CombatRecordSourceKind kind)
        {
            var current = ResolveAttribution(actor);
            var isAbility = current.Kind == CombatRecordSourceKind.Ability;
            return PushAttribution(actor, kind, isAbility ? current.Name : null, isAbility ? current.Code : 0, current.Rank);
        }

        /// <summary>
        /// Resolves who caused a change: the innermost attribution scope, then the actor's active
        /// ability impact, then the actor alone.
        /// </summary>
        private CombatRecordAttribution ResolveAttribution(uint fallbackActor)
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
        public CombatRecordAttribution ResolveOwnAttribution(uint actor)
        {
            if (!GetIsObjectValid(actor))
                return ResolveAttribution(actor);
            if (_attributionDepth > 0 && IsOwnedBy(_attributions[_attributionDepth - 1], actor))
                return _attributions[_attributionDepth - 1];

            return ResolveActiveAbilityAttribution(actor);
        }

        private bool IsOwnedBy(in CombatRecordAttribution scope, uint actor)
        {
            return scope.Actor == actor || scope.Owner == actor;
        }

        private CombatRecordAttribution ResolveActiveAbilityAttribution(uint actor)
        {
            var ability = GetIsObjectValid(actor)
                ? GetActiveAbilityImpactSummary(actor)?.Ability
                : null;
            return ability != null
                ? new CombatRecordAttribution(actor, CombatRecordSourceKind.Ability, ability.Name, GetFeatCode(ability, FeatType.Invalid), ability.AbilityLevel)
                : new CombatRecordAttribution(actor, CombatRecordSourceKind.Unattributed, null, 0, 0);
        }

        /// <summary>
        /// The activated feat when known, else the feat the ability is registered under.
        /// </summary>
        public int GetFeatCode(AbilityDetail ability, FeatType feat)
        {
            if (feat != FeatType.Invalid)
                return (int)feat;

            return _abilityFeats != null && ability != null && _abilityFeats.TryGetValue(ability, out var registeredFeat)
                ? (int)registeredFeat
                : (int)FeatType.Invalid;
        }

        #endregion

        #region Abilities

        /// <summary>
        /// Records a later pulse of a cast, such as a damage field's tick or an emitter's pulse.
        /// </summary>
        public void RecordRecurringAbilityImpact(uint activator, AbilityDetail ability, AbilityImpactSummary summary)
        {
            // A fixed-location pulse keeps landing in the area after its caster leaves.
            if (ability == null || (!_session.IsInRecordedArea(activator) && !_session.AnyInRecordedArea(summary?.ImpactedTargets)))
                return;

            _session.Enqueue(CombatRecordEvent.AbilityImpact(
                _session.GetParticipantId(activator),
                0,
                ability.Name,
                GetFeatCode(ability, FeatType.Invalid),
                ability.AbilityLevel,
                summary?.ImpactedTargetCount ?? 0,
                summary?.CriticalHitCount ?? 0,
                summary?.AttributedDamage ?? 0,
                isRecurring: true));
        }

        /// <param name="attackResult">The native attack result: 1/7 hit, 2 deflect, 3 critical, 4 miss.</param>
        public void RecordAttack(uint attacker, uint defender, int attackResult, int hitChance, string abilityName)
        {
            if (!_session.IsRecorded(defender, attacker))
                return;

            _session.Enqueue(CombatRecordEvent.Attack(
                _session.GetParticipantId(attacker),
                _session.GetParticipantId(defender),
                ToAttackResult(attackResult),
                hitChance,
                abilityName));
        }

        private CombatRecordAttackResult ToAttackResult(int attackResult)
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
        public void ExpectDeferredDamage(uint source, uint target, int amount, CombatDamageDeliveryType deliveryType)
        {
            // A status that outlived its source dispatches its damage from the creature it lands on.
            if (!GetIsObjectValid(source))
                source = target;

            if (amount <= 0 || !_session.IsRecorded(target, source))
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
                                        (IsOwnedBy(scope, source) && GetActiveAbilityImpactSummary(source) == null);
                if (!isDispatchedLater)
                    return;
            }

            // Triggered and damage-over-time hits computed during the source's own ability impact
            // are queued with that impact's hits and land under the scope they were queued in.
            if ((deliveryType == CombatDamageDeliveryType.Triggered || deliveryType == CombatDamageDeliveryType.DamageOverTime) &&
                GetActiveAbilityImpactSummary(source) != null)
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

            _session.AddPendingDamage(source, target, amount, false, attribution);
        }

        /// <summary>
        /// Whether a registration is waiting for damage from <paramref name="source"/> to
        /// <paramref name="target"/>. Observability seam for the engine tests.
        /// </summary>
        public bool HasPendingDamage(uint source, uint target)
        {
            return _session.PendingDamage.Exists(pending => pending.Source == source && pending.Target == target);
        }

        /// <summary>
        /// Registers hit points an ability spends by damaging its own caster.
        /// </summary>
        public void ExpectHitPointCost(uint activator, int amount)
        {
            if (amount <= 0 || !_session.IsInRecordedArea(activator))
                return;

            var attribution = ResolveAttribution(activator);
            attribution.Kind = CombatRecordSourceKind.AbilityCost;
            _session.AddPendingDamage(activator, activator, amount, false, attribution);
        }

        /// <summary>
        /// Registers a landed weapon swing's rolled damage, applied when the attack animation lands.
        /// </summary>
        public void ExpectWeaponDamage(uint attacker, uint defender, int amount, SkillType weaponSkill)
        {
            if (amount <= 0 || !_session.IsRecorded(defender, attacker))
                return;

            var attribution = new CombatRecordAttribution(attacker, CombatRecordSourceKind.Autoattack, null, (int)weaponSkill, 0);
            _session.AddPendingDamage(attacker, defender, amount, true, attribution);
        }

        /// <summary>
        /// Attribution for damage the engine is applying right now: a matching synchronous scope,
        /// then a pending deferred or weapon registration (exact amount first), then the
        /// creator's active ability impact.
        /// </summary>
        private CombatRecordAttribution ResolveDamageAttribution(uint source, uint target, int amount, bool isEngineApplied)
        {
            if (_attributionDepth > 0)
            {
                var scope = _attributions[_attributionDepth - 1];
                if (IsOwnedBy(scope, source))
                    return scope;
            }

            var match = CombatRecordPendingDamageMatcher.FindMatch(
                _session.PendingDamage,
                source,
                target,
                amount,
                isEngineApplied,
                _session.CurrentFrame(),
                _session.UnixTimeMilliseconds(),
                DeferredDamageFrameWindow,
                WeaponDamageWindowMs);
            if (match >= 0)
            {
                var attribution = _session.PendingDamage[match].Attribution;
                _session.PendingDamage.RemoveAt(match);
                return attribution;
            }

            return ResolveActiveAbilityAttribution(source);
        }

        /// <summary>
        /// Called by the native damage hook after the engine applied a damage effect.
        /// Hit points exclude temporary HP, which is reported separately.
        /// </summary>
        /// <param name="isEngineApplied">True when no script or closure applied the effect, as for weapon hits.</param>
        public void RecordDamageApplied(
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
                if (applicationIndex < 0 || applicationIndex >= _session.DamageApplications.Count || !IsRecording)
                    return;

                // A subdued player is resurrected inside the lethal damage call; report the HP the
                // hit left, captured when the death scripts ran.
                var application = _session.DamageApplications[applicationIndex];
                if (application.HasHitPointsLeftByHit)
                    hitPointsAfter = application.HitPointsLeftByHit;

                if (_session.IsRecorded(target, source))
                {
                    // The damage event goes ahead of everything the hit triggered.
                    _session.HeldInsertIndex = application.HeldEventStart;
                    _session.HeldTimestampMs = application.StartedMs;
                    var poolsLeftByHit = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(poolsAfter, application.PoolsAtRemoval);
                    RecordDamageEvents(isEngineApplied, source, target, damage, hitPointsBefore, hitPointsAfter, maxHitPoints,
                        poolsBefore, poolsAfter, poolsLeftByHit);
                }
            }
            finally
            {
                _session.HeldInsertIndex = -1;
                _session.EndDamageApplication(applicationIndex);
            }
        }

        private void RecordDamageEvents(
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
            var sourceId = _session.GetParticipantId(source);
            var targetId = _session.GetParticipantId(target);
            var temporaryBefore = CombatRecordTemporaryHitPointPools.Sum(poolsBefore);
            var temporaryAfter = CombatRecordTemporaryHitPointPools.Sum(poolsAfter);

            _session.Enqueue(CombatRecordEvent.DamageApplied(
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
                    _session.Enqueue(CombatRecordEvent.TemporaryHitPointsAbsorbed(
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
        /// Notes how much the PvP healing reduction took off the heal now being applied to
        /// <paramref name="target"/>, so the heal is recorded with the cut instead of as overheal.
        /// </summary>
        public void RecordPvpHealingReduced(uint target, int requested, int reduced)
        {
            for (var index = _session.HealApplications.Count - 1; index >= 0; index--)
            {
                if (_session.HealApplications[index].Target != target)
                    continue;

                _session.HealApplications[index] = (target, Math.Max(0, requested - reduced));
                return;
            }
        }

        /// <summary>
        /// Called by the native heal hook after the engine applied a heal effect.
        /// </summary>
        public void RecordHealingApplied(uint creator, uint target, int requested, int hitPointsBefore, int hitPointsAfter, int maxHitPoints, int pvpReduced = 0)
        {
            // A heal reacting to a hit still in progress ends what that hit left.
            var application = _session.FindDamageApplication(target);
            if (application >= 0 && !_session.DamageApplications[application].HasHitPointsLeftByHit)
                _session.KeepHitPointsLeftByHit(application, hitPointsBefore);

            if (!_session.IsRecorded(target, creator))
            {
                ReleaseAwaitingHealFor(target);
                return;
            }

            var attribution = ResolveAttribution(creator);
            if (attribution.Kind == CombatRecordSourceKind.Setup)
                return;

            // The cut is not overheal: the heal only ever asked for what was left of it.
            _session.Enqueue(CombatRecordEvent.Healing(
                _session.GetParticipantId(attribution.Actor),
                _session.GetParticipantId(target),
                attribution.Kind,
                attribution.Name,
                attribution.Code,
                Math.Max(0, requested - pvpReduced),
                hitPointsBefore,
                hitPointsAfter,
                maxHitPoints,
                pvpReduced));

            ReleaseAwaitingHealFor(target);
        }

        private void ReleaseAwaitingHealFor(uint target)
        {
            if (_session.AwaitingHealCount > 0 && _session.AwaitingHealTarget == target)
                _session.ReleaseAwaitingHeal();
        }

        /// <summary>
        /// Called before a keyed temporary-HP pool is removed or replaced. During a hit on that
        /// creature, what each pool had left is kept, so the hit's absorption is measured against
        /// that remainder instead of treating the removed pool as fully absorbed.
        /// </summary>
        public void RecordTemporaryHitPointPoolRemoving(uint creature)
        {
            var index = _session.FindDamageApplication(creature);
            if (index < 0)
                return;

            var application = _session.DamageApplications[index];
            application.PoolsAtRemoval = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(
                application.PoolsAtRemoval,
                CaptureTemporaryHitPointPools(creature));
            _session.DamageApplications[index] = application;
        }

        /// <summary>
        /// Records a pool cut back to fit the temporary HP cap, credited to whoever granted it.
        /// </summary>
        public void RecordTemporaryHitPointsTrimmed(uint target, string effectKey, int trimmed, int remaining)
        {
            if (trimmed <= 0 || !_session.IsInRecordedArea(target))
                return;

            var hasGrant = _temporaryHitPointGrants.TryGetValue((target, effectKey), out var grant);
            _session.Enqueue(CombatRecordEvent.TemporaryHitPointsTrimmed(
                _session.GetParticipantId(target),
                effectKey,
                trimmed,
                remaining,
                hasGrant ? grant.SourceId : 0,
                hasGrant ? grant.Attribution.Kind : CombatRecordSourceKind.Unattributed,
                hasGrant ? grant.Attribution.Name : null,
                hasGrant ? grant.Attribution.Code : 0));
        }

        public void RecordTemporaryHitPointsGranted(uint source, uint target, string effectKey, int amount, float durationSeconds)
        {
            if (!_session.IsRecorded(target, source))
            {
                // The new pool replaced any recorded pool with this key; its absorbs are not that granter's.
                _temporaryHitPointGrants.Remove((target, effectKey));
                return;
            }

            // An explicit granter must not inherit another creature's scope, such as the
            // attacker's ability during a defender's reactive shield.
            var attribution = GetIsObjectValid(source) ? ResolveOwnAttribution(source) : ResolveAttribution(target);

            var sourceId = _session.GetParticipantId(attribution.Actor);
            _temporaryHitPointGrants[(target, effectKey)] = (sourceId, attribution);
            _session.Enqueue(CombatRecordEvent.TemporaryHitPointsGranted(
                sourceId,
                _session.GetParticipantId(target),
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
        public void RecordResourceChange(uint creature, CombatRecordResource resource, int delta, int valueAfter)
        {
            if (delta == 0 || !_session.IsInRecordedArea(creature))
                return;

            var attribution = ResolveAttribution(creature);
            _session.Enqueue(CombatRecordEvent.ResourceChanged(
                _session.GetParticipantId(creature), resource, delta, valueAfter,
                attribution.Kind, attribution.Name, attribution.Code));
        }

        /// <summary>
        /// Records an FP or STM reduction that is about to clamp the pool at zero.
        /// </summary>
        public void RecordResourceReduction(uint creature, CombatRecordResource resource, int reduceBy, int currentValue)
        {
            var reduction = Math.Min(Math.Max(0, reduceBy), Math.Max(0, currentValue));
            RecordResourceChange(creature, resource, -reduction, currentValue - reduction);
        }

        /// <summary>
        /// Records a stack reduction made by combat activity (an item use, an ability's cost).
        /// Reductions outside any scope, such as quest turn-ins, are not combat events.
        /// </summary>
        public void RecordItemStackReduced(uint item, int quantity)
        {
            if (_attributionDepth == 0)
                return;

            RecordItemConsumed(GetItemPossessor(item), GetResRef(item), quantity);
        }

        /// <summary>
        /// Records a charge an activated item spent, attributed to that item.
        /// </summary>
        public void RecordItemChargeUsed(uint user, uint item)
        {
            if (!_session.IsInRecordedArea(user))
                return;

            using var attribution = PushItemAttribution(user, item);
            RecordItemConsumed(user, GetResRef(item), 1);
        }

        public void RecordItemConsumed(uint creature, string itemResref, int quantity)
        {
            if (quantity <= 0 || !_session.IsInRecordedArea(creature))
                return;

            var attribution = ResolveAttribution(creature);
            _session.Enqueue(CombatRecordEvent.ResourceChanged(
                _session.GetParticipantId(creature), CombatRecordResource.Item, -quantity, -1,
                attribution.Kind, attribution.Name, attribution.Code, itemResref));
        }

        #endregion

        #region Status effects

        public void RecordStatusApplied(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds, bool isPermanent, bool isRefresh)
        {
            if (statusEffect == null || !_session.IsRecorded(creature, source))
                return;

            _session.Enqueue(CombatRecordEvent.StatusApplied(
                _session.GetParticipantId(source),
                _session.GetParticipantId(creature),
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
        private string FindAbilityScopeName(uint actor)
        {
            for (var depth = _attributionDepth - 1; depth >= 0; depth--)
            {
                var scope = _attributions[depth];
                if (scope.Kind == CombatRecordSourceKind.Ability && scope.Actor == actor)
                    return scope.Name;
            }

            return null;
        }

        public void RecordStatusRemoved(uint creature, IStatusEffect statusEffect, bool isReplacement)
        {
            if (statusEffect == null || !_session.IsRecorded(creature, statusEffect.Source))
                return;

            var reason = isReplacement
                ? CombatRecordStatusRemoval.Replaced
                : statusEffect.WasNaturallyExpired
                    ? CombatRecordStatusRemoval.Expired
                    : CombatRecordStatusRemoval.Removed;
            _session.Enqueue(CombatRecordEvent.StatusRemoved(
                _session.GetParticipantId(statusEffect.Source),
                _session.GetParticipantId(creature),
                statusEffect.Name,
                statusEffect.GetType().Name,
                reason));
        }

        #endregion

        #region Deaths and resurrections

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public void RecordCreatureDeath()
        {
            if (!IsRecording)
                return;

            RecordDeath(OBJECT_SELF, GetLastKiller());
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void RecordPlayerDeath()
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
        public void RecordRespawn(uint player)
        {
            if (!_session.IsInRecordedArea(player))
                return;

            _deathFrames.Remove(player);
            _session.Enqueue(CombatRecordEvent.Resurrection(
                _session.GetParticipantId(player), 0, CombatRecordSourceKind.Respawn, null, 0));
        }

        private void RecordDeath(uint creature, uint killer)
        {
            if (!_session.IsInRecordedArea(creature))
            {
                // A death outside the area still ends the life its last damage source belongs to.
                _session.LastDamageSources.Remove(creature);
                return;
            }

            // A subdual handler that ran first may already have recorded this death.
            if (IsDeathRecorded(creature))
                return;

            // Dying inside an engine damage call: keep the HP the hit left for its damage event.
            // The death event itself is held behind that damage event.
            var index = _session.FindDamageApplication(creature);
            if (index >= 0)
            {
                var application = _session.DamageApplications[index];
                application.DeathRecorded = true;
                _session.DamageApplications[index] = application;
                if (!application.HasHitPointsLeftByHit)
                    KeepHitPointsLeftByHit(index, creature);
            }
            else
            {
                _deathFrames[creature] = _session.CurrentFrame();
            }

            EnqueueDeath(creature, killer);
        }

        /// <summary>
        /// Whether this death was already recorded: by its lethal hit's damage application, or,
        /// outside one, in this frame since the creature's last resurrection.
        /// </summary>
        private bool IsDeathRecorded(uint creature)
        {
            var index = _session.FindDamageApplication(creature);
            if (index >= 0)
                return _session.DamageApplications[index].DeathRecorded;

            return _deathFrames.TryGetValue(creature, out var deathFrame) && deathFrame == _session.CurrentFrame();
        }

        /// <summary>
        /// Keeps the HP a lethal hit left when a handler inside the damage call restores the
        /// creature (a fatal-damage save), so the damage event reports the hit, not the recovery.
        /// </summary>
        public void KeepLethalHitPoints(uint creature)
        {
            var index = _session.FindDamageApplication(creature);
            if (index >= 0 && !_session.DamageApplications[index].HasHitPointsLeftByHit)
                KeepHitPointsLeftByHit(index, creature);
        }

        private void KeepHitPointsLeftByHit(int applicationIndex, uint creature)
        {
            _session.KeepHitPointsLeftByHit(applicationIndex, GetHitPointsExcludingTemporary(creature));
        }

        private void EnqueueDeath(uint creature, uint killer)
        {
            // A resurrected creature starts a new life with no damage source.
            _session.LastDamageSources.Remove(creature, out var lastDamageSource);
            _session.Enqueue(CombatRecordEvent.Death(
                _session.GetParticipantId(creature),
                _session.GetParticipantId(killer),
                _session.GetParticipantId(lastDamageSource)));
        }

        /// <summary>
        /// Records a resurrection, attributed to the innermost scope (for example the reviving
        /// ability's impact) or to <paramref name="kindWithoutScope"/>.
        /// </summary>
        public void RecordResurrection(uint target, CombatRecordSourceKind kindWithoutScope = CombatRecordSourceKind.Unattributed)
        {
            if (!_session.IsInRecordedArea(target))
                return;

            // A new life: a later death in this frame is a real one.
            _deathFrames.Remove(target);
            var attribution = _attributionDepth > 0
                ? _attributions[_attributionDepth - 1]
                : new CombatRecordAttribution(OBJECT_INVALID, kindWithoutScope, null, 0, 0);
            _session.Enqueue(CombatRecordEvent.Resurrection(
                _session.GetParticipantId(target),
                _session.GetParticipantId(attribution.Actor),
                attribution.Kind,
                attribution.Name,
                attribution.Code));
        }

        /// <summary>
        /// Records a subdual and its resurrection. Call before the subdued player is resurrected:
        /// inside the lethal damage call both events wait for that damage and death, and the HP
        /// the hit left is kept for the damage event.
        /// </summary>
        public void RecordSubdual(uint subdued, uint subduedBy)
        {
            if (!_session.IsInRecordedArea(subdued))
                return;

            // The death handlers run in no fixed order, so the death is recorded first either way.
            if (!IsDeathRecorded(subdued))
                RecordDeath(subdued, GetLastHostileActor(subdued));

            var index = _session.FindDamageApplication(subdued);
            if (index >= 0)
                KeepHitPointsLeftByHit(index, subdued);

            EnqueueSubdual(subdued, subduedBy);
        }

        private void EnqueueSubdual(uint subdued, uint subduedBy)
        {
            var subduedId = _session.GetParticipantId(subdued);
            _session.Enqueue(CombatRecordEvent.Subdual(subduedId, _session.GetParticipantId(subduedBy)));
            _session.Enqueue(CombatRecordEvent.Resurrection(subduedId, 0, CombatRecordSourceKind.Subdual, null, 0));
        }

        #endregion

        #region Calls into code that is not converted yet

        // Each wrapper's body is the call the static class made; a test answers it by overriding the method.

        protected virtual void InstallEffectHooks() => CombatRecorderEffectHooks.Install();

        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _catalog.GetAllAbilityDetails();

        protected virtual bool IsFeatRegistered(FeatType feat) => _catalog.IsFeatRegistered(feat);

        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _catalog.GetAbilityDetail(feat);

        protected virtual AbilityImpactSummary GetActiveAbilityImpactSummary(uint creature) =>
            _impactState.GetActiveAbilityImpactSummary(creature);

        protected virtual SkillType GetEquippedWeaponSkillType(uint creature) => _queries.Value.GetEquippedWeaponSkillType(creature);

        protected virtual int GetCurrentFP(uint creature, Player dbPlayer = null) => _stat.Value.GetCurrentFP(creature, dbPlayer);

        protected virtual int GetMaxFP(uint creature, Player dbPlayer = null) => _stat.Value.GetMaxFP(creature, dbPlayer);

        protected virtual int GetCurrentStamina(uint creature, Player dbPlayer = null) => _stat.Value.GetCurrentStamina(creature, dbPlayer);

        protected virtual int GetMaxStamina(uint creature, Player dbPlayer = null) => _stat.Value.GetMaxStamina(creature, dbPlayer);

        protected virtual int GetTemporaryHitPoints(uint creature) => TemporaryHitPointEffects.GetRemaining(creature);

        protected virtual int GetHitPointsExcludingTemporary(uint creature) =>
            CombatRecorderEffectHooks.GetHitPointsExcludingTemporary(creature);

        protected virtual CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) =>
            CombatRecorderEffectHooks.CaptureTemporaryHitPointPools(creature);

        protected virtual string GetLogDirectory() => ApplicationSettings.Get().LogDirectory;

        protected virtual IDisposable ScheduleRepeating(Action task, TimeSpan schedule, TimeSpan delay) =>
            Scheduler.ScheduleRepeating(task, schedule, delay);

        #endregion
    }
}

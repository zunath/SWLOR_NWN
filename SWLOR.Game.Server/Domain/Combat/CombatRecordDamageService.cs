using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage records part of the combat recorder: damage, healing, temporary hit point and death records, with the
    /// handlers that record creature and player deaths. It writes through the session core and reads the attribution stack of
    /// <see cref="ICombatRecorderService"/>. The recorder tells it when a recording starts and stops, so its tables begin each
    /// recording empty.
    /// </summary>
    public class CombatRecordDamageService : ICombatRecordDamageService, ICombatRecordingSessionListener
    {
        // Deferred AssignCommand damage runs on the next AI update, a frame or two later.
        private const ulong DeferredDamageFrameWindow = 10;
        // Weapon damage is rolled before the attack animation lands.
        private const long WeaponDamageWindowMs = 4000;

        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<(uint Target, string Key), (int SourceId, CombatRecordAttribution Attribution)> _temporaryHitPointGrants = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, ulong> _deathFrames = new();

        private readonly ICombatRecordSession _session;
        private readonly ICombatRecorderService _recorder;

        public CombatRecordDamageService(ICombatRecordSession session, ICombatRecorderService recorder)
        {
            _session = session;
            _recorder = recorder;
        }

        /// <summary>
        /// Gives the recorder this part as a listener of its session notices. A reload runs boot handlers that rerun, so the
        /// list is full before the first notice.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void RegisterSessionListener()
        {
            _recorder.RegisterSessionListener(this);
        }

        /// <summary>A recording started: no grant or death frame of an earlier recording carries over.</summary>
        public void OnRecordingStarted()
        {
            _temporaryHitPointGrants.Clear();
            _deathFrames.Clear();
        }

        /// <summary>A recording stopped: the tables are emptied as the recording ends.</summary>
        public void OnRecordingStopped()
        {
            _temporaryHitPointGrants.Clear();
            _deathFrames.Clear();
        }

        #region Damage

        /// <summary>
        /// Attribution for damage the engine is applying right now: a matching synchronous scope,
        /// then a pending deferred or weapon registration (exact amount first), then the
        /// creator's active ability impact.
        /// </summary>
        private CombatRecordAttribution ResolveDamageAttribution(uint source, uint target, int amount, bool isEngineApplied)
        {
            if (_recorder.TryGetInnermostAttribution(out var scope) && _recorder.IsOwnedBy(scope, source))
                return scope;

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

            return _recorder.ResolveActiveAbilityAttribution(source);
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
                if (applicationIndex < 0 || applicationIndex >= _session.DamageApplications.Count || !_session.IsRecording)
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

            var attribution = _recorder.ResolveAttribution(creator);
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
            var attribution = GetIsObjectValid(source) ? _recorder.ResolveOwnAttribution(source) : _recorder.ResolveAttribution(target);

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

        #region Deaths and resurrections

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public void RecordCreatureDeath()
        {
            if (!_session.IsRecording)
                return;

            RecordDeath(OBJECT_SELF, GetLastKiller());
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void RecordPlayerDeath()
        {
            if (!_session.IsRecording)
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
            var attribution = _recorder.TryGetInnermostAttribution(out var scope)
                ? scope
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

        protected virtual int GetHitPointsExcludingTemporary(uint creature) =>
            CombatRecorderEffectHooks.GetHitPointsExcludingTemporary(creature);

        protected virtual CombatRecordTemporaryHitPointPool[] CaptureTemporaryHitPointPools(uint creature) =>
            CombatRecorderEffectHooks.CaptureTemporaryHitPointPools(creature);

        #endregion
    }
}

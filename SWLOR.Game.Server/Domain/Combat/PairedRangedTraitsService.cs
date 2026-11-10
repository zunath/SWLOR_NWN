using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Engine adapter for stat-configured weapon traits and captured ability charges. The logic that used to be static in
    /// <c>Service.PairedRangedTraits</c> is moved here unchanged; the per-actor trait state is this instance's own. The
    /// Twin Pistol mastery service is injected; calls into the unconverted <c>Combat</c>, <c>Ability</c>, <c>Stat</c> and
    /// <c>StatusEffect</c> services sit behind one-line <c>protected virtual</c> methods whose bodies are the original
    /// calls. Reading a stat or a status effect allocates nothing the static code did not.
    /// </summary>
    public class PairedRangedTraitsService : IPairedRangedTraitsService
    {
        private readonly Dictionary<uint, PairedRangedTraitState> _states = new();

        private readonly ITwinPistolMasteryService _mastery;
        private readonly Lazy<IAbilityService> _ability;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ICombatQueriesService _queries;
        private readonly Lazy<ICombatResourcesService> _resources;

        public PairedRangedTraitsService(
            ITwinPistolMasteryService mastery,
            Lazy<IAbilityService> ability,
            IStatService stat,
            IStatusEffectService statusEffect,
            ICombatQueriesService queries,
            Lazy<ICombatResourcesService> resources)
        {
            _mastery = mastery;
            _ability = ability;
            _stat = stat;
            _statusEffect = statusEffect;
            _queries = queries;
            _resources = resources;
        }

        /// <summary>Reports a weapon ability's first landed hit; its chain or own grant adds Cadence once.</summary>
        public void RecordWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation)
        {
            var now = Clock.UtcNow;
            if (activation?.IsCommitted != true || activation.Snapshot.StateGeneration == -1 ||
                activation.CadenceEarnedOnHitAt(now) <= 0 || !CanReceiveTraitEvents(actor))
                return;
            var state = GetState(actor);
            state.RecordAbilityHit(now, GetConfiguration(actor), activation.Snapshot.ActivationId,
                activation.CadenceEarnedOnHitAt(now));
            Sync(actor, state);
        }

        public WeaponAbilityTraitActivation CaptureWeaponAbility(uint actor, AbilityDetail ability)
        {
            if (!WeaponAbilityTraitPolicy.IsHostileWeaponAbility(ability) || !CanReceiveTraitEvents(actor)) return null;
            if (Read(actor, StatType.CadenceMaximum) <= 0 &&
                Read(actor, StatType.CadenceNextAbilityAccuracy) <= 0)
                return new WeaponAbilityTraitActivation(
                    new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), false, false, 0, 0) { StateGeneration = -1 }, 0);
            var configuration = GetConfiguration(actor);
            var damagePerCharge = (ability.WeaponAbilityCadenceDamagePerCharge ?? configuration.CadenceDamagePerCharge) +
                                  configuration.CadenceBonusDamagePerCharge;
            // Without the Cadence trait a foreign ability gains nothing per charge, so it leaves the charges alone.
            var spendsCadence = !ability.KeepsWeaponAbilityCadence && damagePerCharge > 0;
            return new WeaponAbilityTraitActivation(CaptureAbility(actor, true, spendsCadence), damagePerCharge)
                {
                    PrimaryWeapon = GetAbilityAttackWeapon(actor, ability),
                    CadenceOnHit = ability.WeaponAbilityCadenceOnHit
                };
        }

        public void CommitWeaponAbility(uint actor, AbilityDetail ability, WeaponAbilityTraitActivation activation)
        {
            if (activation == null || activation.IsCommitted) return;
            activation.SetPaidStamina(GetAbilityStaminaPaid(actor, ability));
            if (activation.Snapshot.StateGeneration == -1)
            {
                activation.IsCommitted = true;
                return;
            }
            activation.IsCommitted = CommitAbility(actor, activation.Snapshot);
            if (activation.IsCommitted && _states.TryGetValue(actor, out var state))
            {
                activation.ContinuesChain = state.RecordAbilityUse(Clock.UtcNow, GetConfiguration(actor), ChainKey(ability),
                    out var landsBy);
                activation.ChainLandsBy = landsBy;
            }
        }

        /// <summary>Every rank of one ability shares a key, so a rank change is not a different ability.</summary>
        private int ChainKey(AbilityDetail ability) =>
            ability.EffectiveLevelPerkType != PerkType.Invalid
                ? (int)ability.EffectiveLevelPerkType
                : StringComparer.Ordinal.GetHashCode(ability.Name ?? string.Empty) | int.MinValue;

        public void CompleteWeaponAbilityHit(uint actor, WeaponAbilityTraitActivation activation)
        {
            if (activation?.IsCommitted != true || activation.RecoveryCompleted || activation.Snapshot.StateGeneration == -1) return;
            activation.RecoveryCompleted = true;
            CompleteAbility(actor, activation.Snapshot, true, activation.PaidStamina, 0);
        }

        public void CompleteIntrinsicAbility(uint actor, WeaponAbilityTraitActivation activation, bool anyHit,
            int paidStamina, int restoreStamina, int bonusAttempts, int bonusDamagePercent, int bonusSeconds)
        {
            if (activation == null || activation.IntrinsicCompleted) return;
            activation.IntrinsicCompleted = true;
            if (bonusAttempts > 0 && bonusSeconds > 0)
            {
                var state = GetState(actor);
                state.GrantOrdinaryBonus(Clock.UtcNow, bonusAttempts, bonusDamagePercent, bonusSeconds);
                Sync(actor, state);
            }
            if (anyHit && restoreStamina > 0)
                RestoreAbilityHitStamina(actor, GetActiveAbility(actor),
                    Math.Min(Math.Max(0, paidStamina), restoreStamina));
        }

        public PairedRangedOrdinarySnapshot RecordOrdinaryAttempt(uint actor, uint target, long rollId)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            _mastery.RecordTargetChange(actor, target);
            if (!_states.TryGetValue(actor, out var state))
            {
                // Untracked attackers need only the ordinary-shot inputs, which keeps every creature's roll cheap.
                var ordinary = GetOrdinaryConfiguration(actor);
                if (!NeedsOrdinaryState(ordinary))
                    return UntrackedSnapshot(ordinary);
                state = GetState(actor);
            }
            var snapshot = state.RecordOrdinaryAttempt(Clock.UtcNow, GetConfiguration(actor), target, rollId);
            Sync(actor, state);
            return snapshot;
        }

        public void RecordOrdinaryHit(uint actor, uint target, long rollId)
        {
            if (!CanReceiveTraitEvents(actor) || !GetIsObjectValid(target) || target == actor ||
                !GetIsReactionTypeHostile(target, actor))
                return;
            if (!_states.TryGetValue(actor, out var state)) return;
            var stamina = state.RecordOrdinaryHit(Clock.UtcNow, GetConfiguration(actor), target, rollId);
            if (stamina > 0)
                RestoreStamina(actor, stamina);
            Sync(actor, state);
        }

        public int GetOrdinaryDamagePercentAdjustment(uint actor, uint target) =>
            GetSnapshot(actor, target).DamagePercentAdjustment;

        public int GetOrdinaryAccuracy(uint actor) =>
            GetSnapshot(actor, 0).AccuracyAdjustment;

        public void RecordTargetChange(uint actor, uint target)
        {
            _mastery.RecordTargetChange(actor, target);
            if (!_states.TryGetValue(actor, out var state))
                return;
            state.RecordTargetChange(target);
            Sync(actor, state);
        }

        /// <summary>Lets callers skip mitigation work for defenders that have no direct-damage trait.</summary>
        public bool HasDirectHostileDamageTrait(uint actor) =>
            CanReceiveTraitEvents(actor) && Read(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction) > 0;

        /// <summary>Only positive direct hostile damage may call this hook.</summary>
        public void RecordDirectHostileDamage(uint actor)
        {
            if (!HasDirectHostileDamageTrait(actor))
                return;
            var configuration = GetConfiguration(actor);
            if (configuration.DirectDamageNextAbilityStaminaReduction <= 0) return;
            var state = GetState(actor);
            state.RecordDirectHostileDamage(Clock.UtcNow, configuration);
            Sync(actor, state);
        }

        /// <summary>Only a direct hostile accuracy-versus-evasion miss may call this hook.</summary>
        public void RecordAccuracyEvasionMiss(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
                return;
            var configuration = GetConfiguration(actor);
            if (configuration.CadenceMaximum <= 0 || configuration.EvasionCadenceCooldownSeconds <= 0) return;
            var state = GetState(actor);
            state.RecordAccuracyEvasionMiss(Clock.UtcNow, configuration);
            Sync(actor, state);
        }

        public int GetAbilityStaminaCostAdjustment(uint actor)
        {
            if (!CanReceiveTraitEvents(actor) || !_states.TryGetValue(actor, out var state))
                return 0;
            return state.GetAbilityStaminaCostAdjustment(Clock.UtcNow, GetConfiguration(actor));
        }

        public int ConsumeAbilityStaminaDiscount(uint actor)
        {
            if (!CanReceiveTraitEvents(actor) || !_states.TryGetValue(actor, out var state))
                return 0;
            var adjustment = state.ConsumeAbilityStaminaDiscount(Clock.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
            return adjustment;
        }

        public PairedRangedAbilityTraitsSnapshot BeginAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            var snapshot = CaptureAbility(actor, usesNextAbilityAccuracy, spendsCadence);
            CommitAbility(actor, snapshot);
            return snapshot;
        }

        public PairedRangedAbilityTraitsSnapshot CaptureAbility(uint actor, bool usesNextAbilityAccuracy, bool spendsCadence)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedAbilityTraitsSnapshot(Guid.NewGuid(), usesNextAbilityAccuracy, spendsCadence, 0, 0)
                    { StateGeneration = -1 };
            var state = GetState(actor);
            var snapshot = state.CaptureAbility(Clock.UtcNow, GetConfiguration(actor), usesNextAbilityAccuracy, spendsCadence);
            Sync(actor, state);
            return snapshot;
        }

        public bool CommitAbility(uint actor, PairedRangedAbilityTraitsSnapshot snapshot)
        {
            if (!CanReceiveTraitEvents(actor) || !_states.TryGetValue(actor, out var state))
                return false;
            var committed = state.CommitAbility(Clock.UtcNow, GetConfiguration(actor), snapshot);
            Sync(actor, state);
            return committed;
        }

        public void CompleteAbility(
            uint actor, PairedRangedAbilityTraitsSnapshot snapshot, bool anyHit, int paidStamina, int restoreStaminaOnHit,
            int ordinaryBonusAttempts = 0, int ordinaryBonusDamagePercent = 0, int ordinaryBonusSeconds = 0)
        {
            if (!CanReceiveTraitEvents(actor))
                return;
            var state = GetState(actor);
            var restore = state.CompleteAbility(
                Clock.UtcNow, GetConfiguration(actor), snapshot, anyHit, paidStamina, restoreStaminaOnHit, ordinaryBonusAttempts, ordinaryBonusDamagePercent, ordinaryBonusSeconds);
            if (restore > 0)
                RestoreAbilityHitStamina(actor, GetActiveAbility(actor), restore);
            Sync(actor, state);
        }

        public void Reconcile(uint actor)
        {
            if (!_states.TryGetValue(actor, out var state))
                return;
            state.Reconcile(Clock.UtcNow, GetConfiguration(actor));
            Sync(actor, state);
        }

        public void ExitExtendedCadence(uint actor)
        {
            if (!_states.TryGetValue(actor, out var state))
                return;
            var duration = Read(actor, StatType.CadenceDurationSeconds);
            state.ClampCadenceDuration(Clock.UtcNow, duration);
            Sync(actor, state);
        }

        public void ClearCombatState(uint actor) => ClearTransientState(actor);

        public void ClearEquipmentState(uint actor)
        {
            Reconcile(actor);
        }

        public void ClearActor(uint actor)
        {
            ClearTransientState(actor);
            _states.Remove(actor);
        }

        /// <summary>Drops tracking for a destroyed object without refreshing its status effects.</summary>
        public void ForgetDestroyed(uint actor)
        {
            _states.Remove(actor);
            _mastery.ClearTrialState(actor);
        }

        private void ClearTransientState(uint actor)
        {
            _mastery.ClearTrialState(actor);
            if (!_states.TryGetValue(actor, out var state))
                return;
            state.ClearTransientState();
            Sync(actor, state);
        }

        private PairedRangedOrdinarySnapshot GetSnapshot(uint actor, uint target)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedOrdinarySnapshot(0, 0);
            return _states.TryGetValue(actor, out var state)
                ? state.GetOrdinarySnapshot(Clock.UtcNow, GetConfiguration(actor), target)
                : new PairedRangedOrdinarySnapshot(0, 0);
        }

        private bool NeedsOrdinaryState(PairedRangedTraitConfiguration configuration) =>
            configuration.OrdinaryHitRestoreCount > 0 && configuration.OrdinaryHitStaminaRestore > 0 ||
            configuration.RepeatedTargetRequiredHits > 0 && configuration.RepeatedTargetDamagePercent != 0;

        private PairedRangedOrdinarySnapshot UntrackedSnapshot(PairedRangedTraitConfiguration configuration) =>
            new(0, 0);

        /// <summary>The subset of <see cref="GetConfiguration"/> read by <see cref="NeedsOrdinaryState"/> and <see cref="UntrackedSnapshot"/>.</summary>
        private PairedRangedTraitConfiguration GetOrdinaryConfiguration(uint actor) => new()
        {
            OrdinaryHitRestoreCount = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
            OrdinaryHitStaminaRestore = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
            RepeatedTargetRequiredHits = Read(actor, StatType.PairedRangedRepeatedTargetRequiredHits),
            RepeatedTargetDamagePercent = Read(actor, StatType.PairedRangedRepeatedTargetDamagePercent),
        };

        private PairedRangedTraitState GetState(uint actor)
        {
            if (!_states.TryGetValue(actor, out var state))
                _states[actor] = state = new PairedRangedTraitState();
            return state;
        }

        private int Read(uint actor, StatType stat) => GetStatAdjustment(actor, stat);

        // Paired ordinary-damage and Alternating Fire offset the paired firing cycle, so they need two weapons.
        private int ReadDualWield(uint actor, StatType stat) =>
            HasDualWield(actor) ? Read(actor, stat) : 0;

        private bool CanReceiveTraitEvents(uint actor) =>
            GetIsObjectValid(actor) && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0;

        public PairedRangedTraitConfiguration GetConfiguration(uint actor)
        {
            if (!CanReceiveTraitEvents(actor))
                return new PairedRangedTraitConfiguration();
            return new PairedRangedTraitConfiguration
            {
                OrdinaryHitRestoreCount = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitRestoreCount),
                OrdinaryHitStaminaRestore = ReadDualWield(actor, StatType.PairedRangedOrdinaryHitStaminaRestore),
                OrdinaryHitRestoreCooldownSeconds = Read(actor, StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds),
                RepeatedTargetRequiredHits = Read(actor, StatType.PairedRangedRepeatedTargetRequiredHits),
                RepeatedTargetDamagePercent = Read(actor, StatType.PairedRangedRepeatedTargetDamagePercent),
                RepeatedTargetDurationSeconds = Read(actor, StatType.PairedRangedRepeatedTargetDurationSeconds),
                DirectDamageNextAbilityStaminaReduction = Read(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction),
                DirectDamageCostWindowSeconds = Read(actor, StatType.PairedRangedDirectDamageCostWindowSeconds),
                DirectDamageCostCooldownSeconds = Read(actor, StatType.PairedRangedDirectDamageCostCooldownSeconds),
                CadenceMaximum = Read(actor, StatType.CadenceMaximum),
                CadenceDurationSeconds = Read(actor, StatType.CadenceDurationSeconds),
                CadenceChainWindowSeconds = Read(actor, StatType.CadenceChainWindowSeconds),
                CadenceAccuracyPerCharge = Read(actor, StatType.CadenceAccuracyPerCharge),
                CadenceDamagePerCharge = Read(actor, StatType.CadenceDamagePerCharge),
                CadenceBonusDamagePerCharge = Read(actor, StatType.CadenceBonusDamagePerCharge),
                CadenceStaminaRestorePerCharge = Read(actor, StatType.CadenceStaminaRestorePerCharge),
                CadenceStaminaRestoreMaximum = Read(actor, StatType.CadenceStaminaRestoreMaximum),
                CadenceRestoreCooldownSeconds = Read(actor, StatType.CadenceRestoreCooldownSeconds),
                EvasionCadenceCooldownSeconds = Read(actor, StatType.EvasionCadenceCooldownSeconds),
                CadenceNextAbilityAccuracy = Read(actor, StatType.CadenceNextAbilityAccuracy),
                CadenceAccuracyDurationSeconds = Read(actor, StatType.CadenceAccuracyDurationSeconds),
                CadenceAccuracyCooldownSeconds = Read(actor, StatType.CadenceAccuracyCooldownSeconds),
                CadenceExtendedDurationSeconds = Read(actor, StatType.CadenceExtendedDurationSeconds),
            };
        }

        private void Sync(uint actor, PairedRangedTraitState state)
        {
            if (!GetIsObjectValid(actor))
                return;
            var now = Clock.UtcNow;
            var configuration = GetConfiguration(actor);
            Refresh(actor, new CadenceStatusEffect(state.Cadence), Remaining(state.CadenceExpires, now), state.Cadence > 0);
            Refresh(actor, new ConvergingFireStatusEffect(), Remaining(state.RepeatedTargetExpires, now),
                state.HasRepeatedTargetBonus(now, configuration));
            Refresh(actor, new ChangingAnglesStatusEffect(), Remaining(state.CadenceAccuracyExpires, now),
                state.CadenceAccuracyExpires > now);
            Refresh(actor, new CoolUnderFireStatusEffect(), Remaining(state.DamageCostExpires, now),
                state.DamageCostExpires > now);
            Refresh(actor, new TwinSunsStatusEffect(state.BonusAttempts), Remaining(state.BonusExpires, now),
                state.BonusAttempts > 0);
        }

        private float Remaining(DateTime expires, DateTime now) => (float)Math.Max(0d, (expires - now).TotalSeconds);

        private void Refresh<T>(uint actor, T desired, float seconds, bool active) where T : StatusEffectBase
        {
            var existing = GetStatusEffect<T>(actor);
            if (!active || seconds <= 0f)
            {
                if (existing != null)
                    RemoveStatusEffect<T>(actor);
                return;
            }
            if (existing != null && existing.Name == desired.Name && existing.Icon == desired.Icon)
            {
                if (Math.Abs(existing.GetRemainingDurationSeconds(Clock.UtcNow) - seconds) > .01f)
                {
                    existing.SetDurationSeconds(seconds);
                    RefreshStatusEffectDuration(actor, typeof(T), seconds);
                }
                return;
            }
            RemoveStatusEffect<T>(actor);
            ApplyStatusEffect(actor, desired, seconds);
        }

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual uint GetAbilityAttackWeapon(uint actor, AbilityDetail ability) =>
            _queries.GetAbilityAttackWeapon(actor, ability.SkillType);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int GetAbilityStaminaPaid(uint actor, AbilityDetail ability) =>
            _resources.Value.GetAbilityStaminaPaid(actor, ability);

        /// <summary>The unconverted <c>Combat</c> service.</summary>
        protected virtual int RestoreAbilityHitStamina(uint actor, AbilityDetail ability, int requested) =>
            _resources.Value.RestoreAbilityHitStamina(actor, ability, requested);

        /// <summary>The ability of the actor's active ability impact, from the unconverted <c>Ability</c> service.</summary>
        protected virtual AbilityDetail GetActiveAbility(uint actor) =>
            _ability.Value.GetActiveAbilityImpactSummary(actor)?.Ability;

        /// <summary>The unconverted <c>Stat</c> service.</summary>
        protected virtual int GetStatAdjustment(uint actor, StatType stat) => _stat.GetStatAdjustment(actor, stat);

        /// <summary>The unconverted <c>Stat</c> service.</summary>
        protected virtual int RestoreStamina(uint actor, int amount) => _stat.RestoreStamina(actor, amount);

        /// <summary>The unconverted <c>EquipmentPredicates</c> helper.</summary>
        protected virtual bool HasDualWield(uint actor) => EquipmentPredicates.HasDualWield(actor);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual T GetStatusEffect<T>(uint actor) where T : class, IStatusEffect =>
            _statusEffect.GetStatusEffect<T>(actor);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RemoveStatusEffect<T>(uint actor) where T : IStatusEffect =>
            _statusEffect.RemoveStatusEffect<T>(actor);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RefreshStatusEffectDuration(uint actor, Type statusEffectType, float seconds) =>
            _statusEffect.RefreshStatusEffectDuration(actor, statusEffectType, actor, seconds);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void ApplyStatusEffect(uint actor, IStatusEffect effect, float seconds) =>
            _statusEffect.ApplyStatusEffect(actor, actor, effect, seconds);
    }
}

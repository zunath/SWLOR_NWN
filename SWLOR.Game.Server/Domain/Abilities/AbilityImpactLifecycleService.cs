using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The lifecycle of each ability impact (part 5 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md), moved
    /// from <c>AbilityService</c> unchanged: begin (with the damage bonuses it defers until a damaging payload), the recurring and
    /// batched impacts a cast captures, end and abort. It writes the impact state store (<see cref="IAbilityImpactState"/>) the
    /// Combat services read. The start of a telegraph that resolves later is here too, so the telegraphed impact
    /// (<see cref="IAbilityService"/>) keeps to ten services. Calls into the combat services sit behind <c>protected virtual</c>
    /// methods whose bodies are the original calls, so a unit test can answer them.
    /// </summary>
    public class AbilityImpactLifecycleService : IAbilityImpactLifecycleService
    {
        private readonly IAbilityImpactState _impactState;
        private readonly IMeleeTechniqueService _meleeTechnique;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IAbilityDamageDeliveryService _damageDelivery;
        private readonly IStatusEffectCombatEventService _statusEffectCombatEvents;
        private readonly ICombatQueriesService _queries;
        private readonly ICombatAbilityBonusService _abilityBonus;
        private readonly ICombatQueuedAbilityBonusService _queuedAbilityBonus;
        private readonly ICombatAbilityHitRidersService _hitRiders;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatCriticalHitEffectsService _criticalHitEffects;

        public AbilityImpactLifecycleService(
            IAbilityImpactState impactState,
            IMeleeTechniqueService meleeTechnique,
            ICombatRecorderService combatRecorder,
            IAbilityDamageDeliveryService damageDelivery,
            IStatusEffectCombatEventService statusEffectCombatEvents,
            ICombatQueriesService queries,
            ICombatAbilityBonusService abilityBonus,
            ICombatQueuedAbilityBonusService queuedAbilityBonus,
            ICombatAbilityHitRidersService hitRiders,
            ICombatResourcesService resources,
            ICombatCriticalHitEffectsService criticalHitEffects)
        {
            _impactState = impactState;
            _meleeTechnique = meleeTechnique;
            _combatRecorder = combatRecorder;
            _damageDelivery = damageDelivery;
            _statusEffectCombatEvents = statusEffectCombatEvents;
            _queries = queries;
            _abilityBonus = abilityBonus;
            _queuedAbilityBonus = queuedAbilityBonus;
            _hitRiders = hitRiders;
            _resources = resources;
            _criticalHitEffects = criticalHitEffects;
        }

        /// <summary>
        /// Starts impact tracking and defers pending damage bonuses until a damaging payload,
        /// retaining activation-marker snapshots for the impact footprint and flash decision.
        /// </summary>
        public void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            bool countsAsAttackAttempt = true,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            uint triggeringWeapon = OBJECT_INVALID,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null,
            WeaponAbilityTraitActivation weaponTraits = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            BeginAbilityImpact(activator, ability, 0, 0,
                countsAsAttackAttempt: countsAsAttackAttempt,
                activationAreaTelegraphs: activationAreaTelegraphs, sequence: sequence);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (_meleeTechnique.IsSweepActivation(activator, ability))
                trackedImpact.RecordShape(ability.SkillType, true);
            if (weaponTraits != null)
            {
                trackedImpact.Sequence.WeaponTraits = weaponTraits;
            }
            trackedImpact.TriggeringWeaponDamage = triggeringWeaponDamage ?? (GetIsObjectValid(triggeringWeapon)
                ? GetAbilityDMG(activator, triggeringWeapon, ability.SkillType)
                : trackedImpact.TriggeringWeaponDamage);
            if (triggeringWeaponDamageType.HasValue || GetIsObjectValid(triggeringWeapon))
                trackedImpact.TriggeringWeaponDamageType = triggeringWeaponDamageType ?? GetWeaponDamageType(triggeringWeapon);
            trackedImpact.ResolveDamageBonuses = () =>
            {
                var abilitySkillType = GetAbilitySkillType(activator, ability);
                var nextAbilityDamageBonus = ConsumeNextAbilityDamageBonus(activator, ability.EffectiveLevelPerkType);
                var nextSkillAbilityBonuses = ConsumeNextSkillAbilityBonuses(
                    activator,
                    abilitySkillType,
                    ability.ResolveImpactStatusCategories?.Invoke(activator) ?? StatusEffectCategory.None);
                var persistentCriticalRate = GetPersistentNextSkillAbilityCriticalRateBonus(
                    activator,
                    abilitySkillType);
                var queuedWeaponBonuses = ability.ActivationType == AbilityActivationType.Weapon
                    ? ConsumeQueuedWeaponAbilityBonuses(activator, abilitySkillType)
                    : default;
                var guardedHitBonuses = ability.IsHostileAbility
                    ? ConsumeNextAttackGuardedHitBonuses(activator)
                    : (DMGBonus: 0, CriticalRatePercentAdjustment: 0, EnmityBonus: 0);
                var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                    ? GetStatusAppliedNextAttackDamageBonus(activator)
                    : 0;
                trackedImpact.AddDamageBonuses(
                    nextAbilityDamageBonus +
                    nextSkillAbilityBonuses.DamageBonus +
                    guardedHitBonuses.DMGBonus +
                    statusAppliedNextAttackDamageBonus +
                    queuedWeaponBonuses.DamageBonus,
                    nextSkillAbilityBonuses.CriticalRatePercentAdjustment +
                    persistentCriticalRate +
                    queuedWeaponBonuses.CriticalRatePercentAdjustment +
                    guardedHitBonuses.CriticalRatePercentAdjustment,
                    nextSkillAbilityBonuses.DefenseIgnorePercentAdjustment,
                    guardedHitBonuses.EnmityBonus,
                    statusAppliedNextAttackDamageBonus,
                    queuedWeaponBonuses.CriticalDamagePercentAdjustment);
            };
            if (CombatRules.CanDealCombatImpactDamage(0, ability))
                trackedImpact.EnsureDamageBonuses();
        }

        /// <summary>
        /// Registers an impact using already-resolved bonuses and activation-marker geometry.
        /// </summary>
        private void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment = 0,
            int nextAttackEnmityBonus = 0,
            int statusAppliedNextAttackDamageBonus = 0,
            bool countsAsAttackAttempt = true,
            int nextAbilityCriticalDamagePercentAdjustment = 0,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            AbilityImpactTracker sequenceOwner = null,
            bool resolveDamageBonusesFromOwner = false,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null)
        {
            if (!GetIsObjectValid(activator) || ability == null)
                return;

            var abilitySkillType = GetAbilitySkillType(activator, ability);
            var usesQueuedNaturalWeapon =
                ability.ActivationType == AbilityActivationType.Weapon && ability.SkillType == SkillType.BeastMastery;
            _impactState.Track(activator, new AbilityImpactTracker(
                ability,
                nextAbilityDamageBonus,
                nextAbilityCriticalRatePercentAdjustment,
                nextAbilityDefenseIgnorePercentAdjustment,
                nextAttackEnmityBonus,
                statusAppliedNextAttackDamageBonus,
                countsAsAttackAttempt,
                nextAbilityCriticalDamagePercentAdjustment,
                activationAreaTelegraphs,
                sequence)
            {
                SequenceOwner = sequenceOwner?.SequenceOwner ?? sequenceOwner,
                TriggeringWeaponDamage = triggeringWeaponDamage ?? sequenceOwner?.TriggeringWeaponDamage ??
                    _meleeTechnique.GetCapturedWeaponDamage(activator, ability) ?? GetCombatImpactWeaponDamage(
                    activator, abilitySkillType, usesQueuedNaturalWeapon, abilityImpact: true),
                // Captured with the DMG so a delayed impact cannot pair it with a weapon swapped in later.
                TriggeringWeaponDamageType = triggeringWeaponDamageType ?? sequenceOwner?.TriggeringWeaponDamageType ??
                    _meleeTechnique.GetCapturedWeaponDamageType(activator, ability) ??
                    GetCombatImpactWeaponDamageType(activator, abilitySkillType, usesQueuedNaturalWeapon)
            });
            if (resolveDamageBonusesFromOwner && sequenceOwner != null)
            {
                var impact = _impactState.GetTrackedAbilityImpact(activator);
                impact.ResolveDamageBonuses = () =>
                {
                    sequenceOwner.EnsureDamageBonuses();
                    impact.AddDamageBonuses(
                        sequenceOwner.NextAbilityDamageBonus - sequenceOwner.StatusAppliedNextAttackDamageBonus,
                        sequenceOwner.NextAbilityCriticalRatePercentAdjustment,
                        sequenceOwner.NextAbilityDefenseIgnorePercentAdjustment,
                        sequenceOwner.NextAttackEnmityBonus,
                        0,
                        sequenceOwner.NextAbilityCriticalDamagePercentAdjustment);
                };
            }
        }

        /// <summary>
        /// Retains the originating ability and cast sequence for recurring impacts without
        /// consuming another activation's pending bonuses or replacing its impact tracker.
        /// </summary>
        public Action CaptureRepeatedAbilityImpact(
            uint activator, Action impactAction, int baseDamage = 0, bool requiresLivingActivator = true)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            var applyImpact = CaptureRepeatedAbilityImpact<Action>(
                activator, action => action(), baseDamage, requiresLivingActivator);
            return () => applyImpact(impactAction);
        }

        /// <summary>
        /// Captures one cast's recurring impact context while allowing each delayed impact
        /// to retain its own target or payload. Bonuses are consumed only when an impact lands.
        /// </summary>
        public Action<T> CaptureRepeatedAbilityImpact<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0, bool requiresLivingActivator = true,
            bool retainCapturedBonuses = false)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            // Scheduled damage belongs to this cast. Resolve its armed bonuses now so
            // later pulses cannot consume bonuses earned after the field was created.
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var originatingImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (originatingImpact == null)
                return impactAction;

            var ability = originatingImpact.Ability;
            var sequence = originatingImpact.Sequence;
            var copyPartitionedBonuses = retainCapturedBonuses
                ? originatingImpact.CapturePartitionedDamageBonuses() : null;
            return payload =>
            {
                if (!GetIsObjectValid(activator) || requiresLivingActivator && GetCurrentHitPoints(activator) <= 0)
                    return;

                using var recorderAttribution = _combatRecorder.BeginAbilityAttribution(activator, ability);
                var previousImpact = _impactState.GetTrackedAbilityImpact(activator);
                BeginAbilityImpact(activator, ability, 0, 0, countsAsAttackAttempt: false, sequence: sequence,
                    triggeringWeaponDamage: originatingImpact.TriggeringWeaponDamage,
                    triggeringWeaponDamageType: originatingImpact.TriggeringWeaponDamageType);
                if (copyPartitionedBonuses != null)
                    copyPartitionedBonuses(_impactState.GetTrackedAbilityImpact(activator));
                else
                    _impactState.GetTrackedAbilityImpact(activator).CopyRepeatedDamageBonusesFrom(originatingImpact);
                var completed = false;
                try
                {
                    impactAction(payload);
                    var summary = EndAbilityImpact(activator);
                    if (!retainCapturedBonuses)
                        originatingImpact.CompleteRepeatedDamageBonusImpact(summary.ImpactedTargetCount > 0);
                    completed = true;
                    if (_combatRecorder.IsRecording)
                        _combatRecorder.RecordRecurringAbilityImpact(activator, ability, summary);
                    ApplyAbilityImpactEffects(activator, summary);
                }
                finally
                {
                    if (!completed)
                        AbortAbilityImpact(activator);
                    if (previousImpact != null)
                        _impactState.Track(activator, previousImpact);
                }
            };
        }

        public Func<int, Action<T>> CaptureRepeatedAbilityImpactBatch<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0)
        {
            ArgumentNullException.ThrowIfNull(impactAction);
            PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var originatingImpact = _impactState.GetTrackedAbilityImpact(activator);

            return count =>
            {
                AbilityImpactTracker batchImpact = null;
                var batch = new AbilityImpactBatch<T>(count, (payload, isFinalImpact) =>
                {
                    if (originatingImpact == null)
                    {
                        impactAction(payload);
                        return;
                    }

                    if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                        return;

                    using var recorderAttribution = _combatRecorder.BeginAbilityAttribution(activator, originatingImpact.Ability);
                    var previousImpact = _impactState.GetTrackedAbilityImpact(activator);
                    try
                    {
                        if (batchImpact == null)
                        {
                            BeginAbilityImpact(activator, originatingImpact.Ability, 0, 0,
                                countsAsAttackAttempt: false, sequence: originatingImpact.Sequence,
                                triggeringWeaponDamage: originatingImpact.TriggeringWeaponDamage,
                                triggeringWeaponDamageType: originatingImpact.TriggeringWeaponDamageType);
                            batchImpact = _impactState.GetTrackedAbilityImpact(activator);
                            batchImpact.CopyRepeatedDamageBonusesFrom(originatingImpact);
                        }
                        else
                        {
                            _impactState.Track(activator, batchImpact);
                        }

                        impactAction(payload);
                        _impactState.Remove(activator);
                        _damageDelivery.FlushDamageEffects(batchImpact, activator);
                        originatingImpact.CompleteRepeatedDamageBonusImpact(batchImpact.Summary.ImpactedTargetCount > 0);
                        if (isFinalImpact)
                        {
                            _impactState.Track(activator, batchImpact);
                            var summary = EndAbilityImpact(activator);
                            if (_combatRecorder.IsRecording)
                                _combatRecorder.RecordRecurringAbilityImpact(activator, originatingImpact.Ability, summary);
                            ApplyAbilityImpactEffects(activator, summary);
                        }
                    }
                    finally
                    {
                        _impactState.Remove(activator);
                        if (previousImpact != null)
                            _impactState.Track(activator, previousImpact);
                    }
                });
                return batch.Apply;
            };
        }

        public AbilityImpactSummary EndAbilityImpact(uint activator)
        {
            if (!_impactState.TryGetTrackedAbilityImpact(activator, out var impact))
                return new AbilityImpactSummary();

            _impactState.Remove(activator);
            _damageDelivery.FlushDamageEffects(impact, activator);
            if (impact.Ability.IsHostileAbility && impact.Summary.CriticalHitCount > 0)
            {
                RefundCriticalRangedAbilityStaminaCost(
                    activator,
                    impact.Ability);
            }

            if (impact.Ability.IsHostileAbility && impact.CountsAsAttackAttempt)
            {
                if (impact.Summary.CriticalHitCount > 0)
                {
                    ConsumePersistentNextSkillAbilityCriticalRateBonus(
                        activator,
                        impact.Summary.SkillType);
                }
                else
                {
                    ApplyNonCriticalAbilityEffects(
                        activator,
                        OBJECT_INVALID,
                        impact.Summary.SkillType);
                }

                _statusEffectCombatEvents.NotifyAttackAttemptStatusEffects(
                    activator,
                    impact.Summary.SkillType,
                    impact.Summary);
            }
            _impactState.SetLastCompletedAbilityImpactSummary(activator, impact.Summary);
            _meleeTechnique.CompleteActivation(activator, impact.Summary);
            return impact.Summary;
        }

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        public void AbortAbilityImpact(uint activator)
        {
            if (!_impactState.TryGetTrackedAbilityImpact(activator, out var impact))
                return;

            _impactState.Remove(activator);
            Log.WriteStructured(
                LogGroup.Error,
                "Ability impact aborted for activator {Activator}. Ability: {Ability}.",
                activator,
                impact.Ability?.Name ?? "Unknown");
        }

        /// <summary>
        /// Begins the impact of a telegraph that resolves after its cast, with the bonuses the cast captured and the status-applied
        /// bonus the activator holds when it lands; the bonuses the cast left pending resolve from its impact at the first
        /// damaging payload.
        /// </summary>
        public void BeginTelegraphedAbilityImpact(
            uint creator,
            AbilityDetail ability,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            AbilityImpactTracker sequenceOwner,
            bool resolveDamageBonusesFromOwner)
        {
            var statusAppliedNextAttackDamageBonus = ability.IsHostileAbility
                ? GetStatusAppliedNextAttackDamageBonus(creator)
                : 0;
            BeginAbilityImpact(
                creator,
                ability,
                nextAbilityDamageBonus + statusAppliedNextAttackDamageBonus,
                nextAbilityCriticalRatePercentAdjustment,
                nextAbilityDefenseIgnorePercentAdjustment,
                nextAttackEnmityBonus,
                statusAppliedNextAttackDamageBonus,
                countsAsAttackAttempt: false,
                sequenceOwner: sequenceOwner,
                resolveDamageBonusesFromOwner: resolveDamageBonusesFromOwner);
        }

        public void PrepareCombatImpactDamageBonuses(uint activator, int baseDamage)
        {
            var impact = _impactState.GetTrackedAbilityImpact(activator);
            if (impact != null && CombatRules.CanDealCombatImpactDamage(baseDamage, impact.Ability))
                impact.EnsureDamageBonuses();
        }

        protected virtual int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill) => WeaponDamage.GetAbilityDMG(creature, weapon, abilitySkill);

        protected virtual CombatDamageType GetWeaponDamageType(uint weapon) => WeaponDamage.GetDamageType(weapon);

        protected virtual SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => _queries.GetAbilitySkillType(creature, ability);

        protected virtual int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType) => _abilityBonus.ConsumeNextAbilityDamageBonus(creature, perkType);

        protected virtual (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(uint creature, SkillType skillType, StatusEffectCategory abilityStatusCategories) =>
            _abilityBonus.ConsumeNextSkillAbilityBonuses(creature, skillType, abilityStatusCategories);

        protected virtual int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) => _abilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        protected virtual (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(uint creature, SkillType skillType) =>
            _queuedAbilityBonus.ConsumeQueuedWeaponAbilityBonuses(creature, skillType);

        protected virtual (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature) =>
            _abilityBonus.ConsumeNextAttackGuardedHitBonuses(creature);

        protected virtual int GetStatusAppliedNextAttackDamageBonus(uint creature) => _abilityBonus.GetStatusAppliedNextAttackDamageBonus(creature);

        protected virtual int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
            _queries.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, triggeringWeaponDamage: triggeringWeaponDamage, abilityImpact: abilityImpact);

        protected virtual CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon) =>
            _queries.GetCombatImpactWeaponDamageType(activator, skillType, usesQueuedNaturalWeapon);

        protected virtual void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) => _hitRiders.ApplyAbilityImpactEffects(activator, summary);

        protected virtual int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability) => _resources.RefundCriticalRangedAbilityStaminaCost(creature, ability);

        protected virtual int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) => _abilityBonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        protected virtual void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) => _criticalHitEffects.ApplyNonCriticalAbilityEffects(activator, target, skillType);
    }
}

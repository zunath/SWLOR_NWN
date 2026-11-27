using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// What a hostile ability's hit does to its target once its damage is known (part 12 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from <c>AbilityService</c> unchanged apart from how it reaches its
    /// dependencies: the battle theme and the capstone proof are notices to <see cref="IAbilityImpactObserver"/>, the damage block
    /// (with the status-applied bonus consume) is the delivery part's, and the success visual is the visual part's. Calls into
    /// the combat services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can
    /// answer them.
    /// </summary>
    public class AbilityHostileImpactService : IAbilityHostileImpactService
    {
        private const int HostileAbilityBaseEnmity = 100;

        private readonly IAbilityImpactState _impactState;
        private readonly IAbilityImpactObserver[] _observers;
        private readonly ICombatPointService _combatPoint;
        private readonly IEnmityChangeService _enmityChange;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAbilityImpactService _abilityImpact;
        private readonly IAbilityDamageDeliveryService _damageDelivery;
        private readonly IAbilityStatusApplicationService _statusApplication;
        private readonly IAbilityDarkForceService _darkForce;
        private readonly IAbilityImpactVisualService _impactVisual;

        public AbilityHostileImpactService(
            IAbilityImpactState impactState,
            IEnumerable<IAbilityImpactObserver> observers,
            ICombatPointService combatPoint,
            IEnmityChangeService enmityChange,
            ICombatResourcesService resources,
            ICombatAbilityImpactService abilityImpact,
            IAbilityDamageDeliveryService damageDelivery,
            IAbilityStatusApplicationService statusApplication,
            IAbilityDarkForceService darkForce,
            IAbilityImpactVisualService impactVisual)
        {
            _impactState = impactState;
            // Copied once, so a hit notifies them without enumerating the sequence again.
            _observers = observers.ToArray();
            _combatPoint = combatPoint;
            _enmityChange = enmityChange;
            _resources = resources;
            _abilityImpact = abilityImpact;
            _damageDelivery = damageDelivery;
            _statusApplication = statusApplication;
            _darkForce = darkForce;
            _impactVisual = impactVisual;
        }

        public void ApplyHostileAbilityEnmity(uint activator, uint target, int bonus = 0)
        {
            // Damage is credited once by the actual damage event, including resistance and absorption.
            var profile = WeaponImpactProfile.Get(activator);
            var amount = CalculateHostileAbilityEnmity(bonus, profile?.VolleyIndex ?? 0, profile?.VolleyCount ?? 1);
            _enmityChange.ModifyEnmity(activator, target, amount);
        }

        public int CalculateHostileAbilityEnmity(int bonus, int volleyIndex = 0, int volleyCount = 1) =>
            WeaponImpactDamagePartition.Partition(HostileAbilityBaseEnmity + Math.Max(0, bonus), volleyIndex, volleyCount);

        public int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            Type statusEffect = null,
            int duration = 0,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool firstHostileAbilityHitDamageBonusApplied = false)
        {
            using var damageDerivedHealing = BeginDamageDerivedHealing(activator, target);
            // Read before this hit's damage and enmity can turn the target onto the activator.
            var targetWasAttackingAlly = IsTargetAttackingAllyForStatusAppliedRiders(activator, target);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var allowsHitProcs = trackedImpact?.Ability.MeleeTechnique == null ||
                trackedImpact.Sequence.TryTriggerDamageRider($"TechniqueHitProcs:{target}");
            // Hostile abilities raise no attack or damage events when they only apply status effects.
            foreach (var observer in _observers)
                observer.OnTargetEngaged(activator, target);

            // Register the combat point before applying damage. A lethal hit resolves the target's
            // death (and its skill XP distribution) synchronously during EffectDamage below, so
            // registering afterward would miss the payout entirely when an ability one-shots a target.
            if (awardsCombatPoints)
                _combatPoint.AddCombatPoint(activator, target, skillType, 3);

            if (damage > 0)
            {
                _damageDelivery.DeliverHostileImpactDamage(
                    activator,
                    target,
                    skillType,
                    damage,
                    damageType,
                    effectDamageType,
                    trackedImpact,
                    allowsHitProcs);
            }

            ApplyHostileAbilityEnmity(
                activator,
                target,
                Math.Max(0, enmityBonus) + Math.Max(0, trackedImpact?.NextAttackEnmityBonus ?? 0));

            var statusApplied = _statusApplication.ApplyCombatImpactStatusEffect(
                activator,
                target,
                skillType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                damageType,
                out var appliedStatusCategories);
            if (statusApplied)
            {
                _darkForce.ApplyDarkForceCastConversion(activator, target);
            }

            // Statuses the hook applies on this hit (conditional statuses) count like the impact's own
            // status, so category-keyed perks such as "after you apply a control effect" see them.
            if (beforeSuccessfulImpactRiders?.Invoke(target) is { } riderStatusCategories)
            {
                statusApplied = true;
                appliedStatusCategories |= riderStatusCategories;
            }

            foreach (var observer in _observers)
                observer.OnAbilityImpact(activator, target, trackedImpact?.Ability);

            ApplySuccessfulAbilityImpactRiders(
                activator,
                target,
                trackedImpact?.Ability,
                skillType,
                damageType,
                damage,
                statusApplied,
                statusEffect,
                additionalStatusEffects,
                firstHostileAbilityHitDamageBonusApplied,
                trackedImpact == null || trackedImpact.Summary.ImpactedTargetCount == 0,
                appliedStatusCategories,
                allowsHitProcs,
                targetWasAttackingAlly);

            if (damage > 0 && trackedImpact?.Ability is { } impactAbility &&
                impactAbility.DamageImpactVisualEffect != VisualEffect.None)
            {
                var damageVisual = EffectVisualEffect(impactAbility.DamageImpactVisualEffect);
                ApplyEffectToObject(DurationType.Instant, damageVisual, target);
            }

            if (damage > 0 || statusApplied)
            {
                if (trackedImpact?.VisualEffects.Effect is { } authoredEffect && authoredEffect != VisualEffect.None)
                    _impactVisual.PlaySuccessfulImpactVisualEffect(_impactState.GetTrackedAbilityImpact(activator)?.VisualEffects, target);
                else if (targetVisualEffect != VisualEffect.None)
                    VisualEffectPlayback.ApplyToObject(targetVisualEffect, target);
            }

            afterSuccessfulHit?.Invoke(target);
            _impactState.RecordAbilityImpactTarget(activator, target, skillType, false);
            return damage;
        }

        /// <summary>
        /// Adds this impact's captured flat bonus to an already-scaled damage payload before
        /// target mitigation. Control-only payloads remain zero.
        /// </summary>
        public int ApplyCapturedAbilityDamageBonus(uint activator, int damage)
        {
            return damage > 0 ? damage + (_impactState.GetTrackedAbilityImpact(activator)?.NextAbilityDamageBonus ?? 0) : 0;
        }

        protected virtual IDisposable BeginDamageDerivedHealing(uint creature, uint target) => _resources.BeginDamageDerivedHealing(creature, target);

        protected virtual bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target) => _abilityImpact.IsTargetAttackingAllyForStatusAppliedRiders(activator, target);

        protected virtual void ApplySuccessfulAbilityImpactRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType, CombatDamageType damageType, int damage, bool statusApplied, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, bool firstHostileAbilityHitDamageBonusApplied, bool isFirstSuccessfulTarget, StatusEffectCategory appliedStatusCategories, bool allowsHitProcs, bool targetWasAttackingAlly) =>
            _abilityImpact.ApplySuccessfulAbilityImpactRiders(activator, target, ability, skillType, damageType, damage, statusApplied, primaryStatusEffect, additionalStatusEffects, firstHostileAbilityHitDamageBonusApplied, isFirstSuccessfulTarget, appliedStatusCategories, allowsHitProcs, targetWasAttackingAlly);
    }
}

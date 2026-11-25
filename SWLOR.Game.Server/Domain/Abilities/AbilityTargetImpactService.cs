using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The per-target resolution of a hostile ability impact (part 11 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from the private <c>ApplyHostileCombatImpact</c> of
    /// <c>AbilityService</c> unchanged apart from one split: the method rolled the hit, then computed the damage, then applied it,
    /// and reached more than ten services. It keeps the damage-derived healing scope around everything, asks
    /// <see cref="IAbilityHitRollService"/> for the hit roll (whose values come back in an <see cref="AbilityHitRoll"/>, read into
    /// locals of the original names), computes the damage, and hands the hit to <see cref="IAbilityHostileImpactService"/>. Calls
    /// into the combat services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a unit test can
    /// answer them.
    /// </summary>
    public class AbilityTargetImpactService : IAbilityTargetImpactService
    {
        private readonly IAbilityImpactState _impactState;
        private readonly IAbilityHitRollService _hitRoll;
        private readonly IAbilityHostileImpactService _hostileImpact;
        private readonly IAbilityDarkForceService _darkForce;
        private readonly IAbilityDamageInputService _damageInput;
        private readonly IAbilityDamageStageService _damageStage;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAbilityDamageBonusService _damageBonus;
        private readonly ICombatAbilityBonusService _abilityBonus;

        public AbilityTargetImpactService(
            IAbilityImpactState impactState,
            IAbilityHitRollService hitRoll,
            IAbilityHostileImpactService hostileImpact,
            IAbilityDarkForceService darkForce,
            IAbilityDamageInputService damageInput,
            IAbilityDamageStageService damageStage,
            ICombatResourcesService resources,
            ICombatAbilityDamageBonusService damageBonus,
            ICombatAbilityBonusService abilityBonus)
        {
            _impactState = impactState;
            _hitRoll = hitRoll;
            _hostileImpact = hostileImpact;
            _darkForce = darkForce;
            _damageInput = damageInput;
            _damageStage = damageStage;
            _resources = resources;
            _damageBonus = damageBonus;
            _abilityBonus = abilityBonus;
        }

        public int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            using var damageDerivedHealing = BeginDamageDerivedHealing(activator, target);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (!_hitRoll.TryResolveHostileImpactHit(
                    activator,
                    target,
                    skillType,
                    damageType,
                    statusEffect,
                    additionalStatusEffects,
                    statusEffectFactory,
                    hitChancePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    combatImpactDamageAbility,
                    resolvesHit,
                    useUnscaledDamage,
                    trackedImpact,
                    out var hit))
                return 0;

            damageType = hit.DamageType;
            var usesNPCStatScaling = hit.UsesNPCStatScaling;
            var damageAbility = hit.DamageAbility;
            var appliedStatusCategories = hit.AppliedStatusCategories;
            var adjustedBaseDamage = Math.Max(0, baseDamage + (baseDamageAdjustment?.Invoke(target) ?? 0));
            var impactDamage = ResolveCombatImpactBaseDamage(
                adjustedBaseDamage,
                trackedImpact?.Ability,
                () =>
                {
                    trackedImpact?.EnsureDamageBonuses();
                    return GetAbilityImpactBaseDamageBonus(
                          activator, target, trackedImpact?.Ability, skillType) +
                      GetAbilityStatusCategoryDamageBonus(
                          activator, skillType, appliedStatusCategories);
                });
            adjustedBaseDamage = impactDamage.BaseDamage;
            var authoredDamagePercentAdjustment = impactDamage.DealsDamage ? damagePercentAdjustment?.Invoke(target) ?? 0 : 0;
            var lowHPDamagePercentAdjustment = impactDamage.DealsDamage ? _darkForce.GetDarkForceTargetLowHPDamageAdjustment(activator, target) : 0;
            var damage = !impactDamage.DealsDamage ? 0 : useUnscaledDamage
                ? _damageInput.CalculateUnscaledCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment)
                : usesNPCStatScaling
                    ? _damageStage.CalculateNPCCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment)
                    : _damageStage.CalculateCombatImpactDamage(activator, target, skillType, adjustedBaseDamage, damageType, criticalRatePercentAdjustment, damageAbility, canCritical, authoredDamagePercentAdjustment, lowHPDamagePercentAdjustment);
            return _hostileImpact.ApplyHostileCombatImpact(
                activator,
                target,
                skillType,
                damage,
                damageType,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                statusResistanceType,
                targetVisualEffect,
                enmityBonus,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                awardsCombatPoints,
                effectDamageType,
                firstHostileAbilityHitDamageBonusApplied: impactDamage.DealsDamage);
        }

        private (int BaseDamage, bool DealsDamage) ResolveCombatImpactBaseDamage(
            int baseDamage,
            AbilityDetail ability,
            Func<int> getDamageBonus)
        {
            // Queued weapon damage and explicitly declared deferred damage remain eligible even
            // with zero immediate base damage. Control-only impacts must not consume bonuses.
            var dealsDamage = CombatRules.CanDealCombatImpactDamage(baseDamage, ability);
            return dealsDamage ? (baseDamage + getDamageBonus(), true) : (0, false);
        }

        protected virtual IDisposable BeginDamageDerivedHealing(uint creature, uint target) => _resources.BeginDamageDerivedHealing(creature, target);

        protected virtual int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) => _damageBonus.GetAbilityImpactBaseDamageBonus(activator, target, ability, skillType);

        protected virtual int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => _abilityBonus.GetAbilityStatusCategoryDamageBonus(creature, skillType, appliedCategories);
    }
}

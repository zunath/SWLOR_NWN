using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The status application of an ability impact (part 18 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md), moved from <c>AbilityService</c> unchanged. The duration adjustment of the
    /// activator's ability bonuses sits behind a <c>protected virtual</c> method whose body is the original call, so a unit test
    /// can answer it.
    /// </summary>
    public class AbilityStatusApplicationService : IAbilityStatusApplicationService
    {
        private readonly IStatusEffectService _statusEffect;
        private readonly IResistanceService _resistance;
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatAbilityBonusService _abilityBonus;

        public AbilityStatusApplicationService(
            IStatusEffectService statusEffect,
            IResistanceService resistance,
            IAbilityImpactState impactState,
            ICombatAbilityBonusService abilityBonus)
        {
            _statusEffect = statusEffect;
            _resistance = resistance;
            _impactState = impactState;
            _abilityBonus = abilityBonus;
        }

        public StatusEffectCategory GetCombatImpactStatusCategories(
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var categories = GetStatusEffectTypeCategories(statusEffect);

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects)
                {
                    categories |= GetStatusEffectTypeCategories(additionalStatusEffect);
                }
            }

            var factoryStatusEffect = statusEffectFactory?.Invoke();
            if (factoryStatusEffect != null)
            {
                categories |= factoryStatusEffect.Categories;
            }

            return categories;
        }

        private StatusEffectCategory GetStatusEffectTypeCategories(Type statusEffect)
        {
            if (statusEffect == null)
                return StatusEffectCategory.None;

            var categories = StatusEffectCategory.None;
            foreach (StatusEffectCategory category in Enum.GetValues(typeof(StatusEffectCategory)))
            {
                if (category == StatusEffectCategory.None)
                    continue;

                if (_statusEffect.HasStatusEffectCategory(statusEffect, category))
                    categories |= category;
            }

            return categories;
        }

        public bool ApplyCombatImpactStatusEffect(
            uint activator,
            uint target,
            SkillType skillType,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType,
            out StatusEffectCategory appliedStatusCategories)
        {
            appliedStatusCategories = StatusEffectCategory.None;
            var hasAdditionalStatusEffects = additionalStatusEffects?.Any(x => x != null) ?? false;
            if (duration <= 0 || (statusEffect == null && statusEffectFactory == null && !hasAdditionalStatusEffects))
                return false;

            duration = ApplyAbilityStatusDurationAdjustment(
                activator,
                duration,
                skillType,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            var statusApplied = false;
            if (statusEffectFactory != null)
            {
                var applied = ApplyCombatImpactTrackedStatusEffect(
                    activator, target, statusEffectFactory, duration, statusResistanceType, sourceDamageType, out var categories);
                statusApplied |= applied;
                if (applied)
                    appliedStatusCategories |= categories;
            }
            else if (statusEffect != null)
            {
                var applied = ApplyCombatImpactTrackedStatusEffect(
                    activator, target, statusEffect, duration, statusResistanceType, sourceDamageType, out var categories);
                statusApplied |= applied;
                if (applied)
                    appliedStatusCategories |= categories;
            }

            if (additionalStatusEffects != null)
            {
                foreach (var additionalStatusEffect in additionalStatusEffects.Where(x => x != null && x != statusEffect).Distinct())
                {
                    var applied = ApplyCombatImpactTrackedStatusEffect(
                        activator, target, additionalStatusEffect, duration, statusResistanceType, sourceDamageType, out var categories);
                    statusApplied |= applied;
                    if (applied)
                        appliedStatusCategories |= categories;
                }
            }

            return statusApplied;
        }

        private int ApplyAbilityStatusDurationAdjustment(
            uint activator,
            int duration,
            SkillType skillType,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            if (duration <= 0)
                return duration;

            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var perkType = trackedImpact?.Ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            var adjustment = GetAbilityStatusDurationPercentAdjustment(
                activator,
                perkType,
                skillType,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory);
            if (adjustment == 0)
                return duration;

            return Math.Max(1, duration + (int)Math.Ceiling(duration * (adjustment / 100f)));
        }

        private bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Type type,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType,
            out StatusEffectCategory appliedCategories)
        {
            var applied = _resistance.IsValidResistanceType(statusResistanceType)
                ? _statusEffect.ApplyStatusEffect(activator, target, type, duration, statusResistanceType)
                : _statusEffect.ApplyStatusEffect(activator, target, type, duration, sourceDamageType);
            appliedCategories = applied
                ? _statusEffect.GetStatusEffect(target, type, activator)?.Categories ?? StatusEffectCategory.None
                : StatusEffectCategory.None;
            return applied;
        }

        private bool ApplyCombatImpactTrackedStatusEffect(
            uint activator,
            uint target,
            Func<IStatusEffect> statusEffectFactory,
            float duration,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType,
            out StatusEffectCategory appliedCategories)
        {
            var statusEffect = statusEffectFactory?.Invoke();
            if (statusEffect == null)
            {
                appliedCategories = StatusEffectCategory.None;
                return false;
            }

            var applied = _resistance.IsValidResistanceType(statusResistanceType)
                ? _statusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, statusResistanceType)
                : _statusEffect.ApplyStatusEffect(activator, target, statusEffect, duration, sourceDamageType);
            appliedCategories = applied ? statusEffect.Categories : StatusEffectCategory.None;
            return applied;
        }

        protected virtual int GetAbilityStatusDurationPercentAdjustment(uint creature, PerkType perkType, SkillType skillType, Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, Func<IStatusEffect> statusEffectFactory) =>
            _abilityBonus.GetAbilityStatusDurationPercentAdjustment(creature, perkType, skillType, primaryStatusEffect, additionalStatusEffects, statusEffectFactory);
    }
}

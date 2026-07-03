using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class QueuedAbilityBonuses
    {
        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = AbilityImpactEffects.GetPerkTypeFromStat(perkTypeValue);
            AbilityImpactEffects.GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(skillTypeValue);
            AbilityImpactEffects.GrantNextSkillAbilityBonuses(
                creature,
                skillType,
                damageBonus,
                criticalRatePercentAdjustment,
                durationSeconds);
        }

        public static int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityDamageBonus,
                AbilityImpactEffects.GetPerkTypeGroup(perkType));
        }

        public static int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                AbilityImpactEffects.GetPerkTypeGroup(perkType));
        }

        public static int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                AbilityImpactEffects.GetPerkTypeGroup(perkType));
        }

        public static int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType)
        {
            var adjustment = AbilityImpactEffects.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDamageFlatAdjustmentPerkType,
                StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
                StatType.AbilityDamageFlatAdjustment);

            adjustment += CombatDamageCalculator.GetRangedAttackDamageFlatAdjustment(creature, skillType);

            return adjustment;
        }

        public static int GetAbilityStatusCategoryDamageBonus(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return QueuedAbilityBonuses.GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryDamageBonus);
        }

        public static int GetAbilityStatusCategoryHitChancePercentAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return QueuedAbilityBonuses.GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryHitChancePercentAdjustment);
        }

        internal static int GetAbilityStatusCategoryAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories,
            StatType adjustmentStatType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || appliedCategories == StatusEffectCategory.None)
                return 0;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredCategories = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusRequiredCategory));
            if (requiredCategories == StatusEffectCategory.None ||
                (appliedCategories & requiredCategories) == 0)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, adjustmentStatType);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType)
        {
            return AbilityImpactEffects.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStaminaCostFlatAdjustmentPerkType,
                StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType,
                StatType.AbilityStaminaCostFlatAdjustment);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return 0;

            var adjustment = QueuedAbilityBonuses.GetAbilityStaminaCostFlatAdjustment(creature, ability.EffectiveLevelPerkType);
            var skillType = QueuedCombatActions.GetAbilitySkillType(creature, ability);
            var flatSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType));
            if (AbilityImpactEffects.SkillTypeMatches(skillType, flatSkillType))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.SkillAbilityStaminaCostFlatAdjustment);
            }

            var highResourceSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.HighResourceAbilityStaminaCostSkillType));
            var threshold = Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostThresholdPercent);
            if (threshold > 0 &&
                AbilityImpactEffects.SkillTypeMatches(skillType, highResourceSkillType) &&
                AbilityImpactEffects.IsCurrentFPAtOrAbovePercent(creature, threshold))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostAdjustment);
            }

            return adjustment;
        }

        public static void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (staminaCost <= 0 || ability == null)
                return;

            var skillType = QueuedCombatActions.GetAbilitySkillType(creature, ability);
            TrackAbilityStaminaCost(creature, ability, staminaCost);
            var restoreSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStaminaCostFPRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityStaminaCostFPRestorePercent);
            if (restorePercent <= 0 || !AbilityImpactEffects.SkillTypeMatches(skillType, restoreSkillType))
                return;

            Stat.RestoreFP(creature, QueuedAbilityBonuses.CalculateResourceRestoreFromCost(staminaCost, restorePercent));
        }

        internal static void TrackAbilityStaminaCost(uint creature, AbilityDetail ability, int staminaCost)
        {
            if (!GetIsObjectValid(creature) ||
                ability?.IsHostileAbility != true ||
                staminaCost <= 0)
            {
                return;
            }

            CombatState.TrackAbilityStaminaCost(creature, staminaCost);
        }

        public static void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost)
        {
            if (fpCost <= 0 || ability == null)
                return;

            var skillType = QueuedCombatActions.GetAbilitySkillType(creature, ability);
            var restoreSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityFPCostStaminaRestorePercentSkillType));
            var restorePercent = Stat.GetStatAdjustment(creature, StatType.AbilityFPCostStaminaRestorePercent);
            if (restorePercent <= 0 || !AbilityImpactEffects.SkillTypeMatches(skillType, restoreSkillType))
                return;

            Stat.RestoreStamina(creature, QueuedAbilityBonuses.CalculateResourceRestoreFromCost(fpCost, restorePercent));
        }

        internal static int CalculateResourceRestoreFromCost(int cost, int percent)
        {
            if (cost <= 0 || percent <= 0)
                return 0;

            return Math.Max(1, (int)Math.Ceiling(cost * (percent / 100f)));
        }

        public static int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextAbilityFPCostAdjustment,
                    StatType.NextAbilityFPCostAdjustmentSkillType)
                : 0;
        }

        public static int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = QueuedAbilityBonuses.GetNextAbilityFPCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                StatType.NextAbilityFPCostAdjustmentSkillType);

            return adjustment;
        }

        public static int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var adjustment = AbilityImpactEffects.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStatusDurationPercentAdjustmentPerkType,
                StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType,
                StatType.AbilityStatusDurationPercentAdjustment);
            adjustment += QueuedAbilityBonuses.GetIdleStatusDurationPercentAdjustment(
                creature,
                skillType,
                primaryStatusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            return adjustment;
        }

        internal static int GetIdleStatusDurationPercentAdjustment(
            uint creature,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationPercentAdjustmentSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredIdleSeconds = Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationRequiredIdleSeconds);
            if (requiredIdleSeconds <= 0 || CombatActivity.HasRecentAttackActivity(creature, requiredIdleSeconds))
                return 0;

            var requiredCategory = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationRequiredCategory));
            if (requiredCategory != 0 &&
                !QueuedAbilityBonuses.StatusContextHasCategory(primaryStatusEffect, additionalStatusEffects, statusEffectFactory, requiredCategory))
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationPercentAdjustment);
        }

        internal static bool StatusContextHasCategory(
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            StatusEffectCategory category)
        {
            if (WeaponStatusImpactEffects.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, category))
                return true;

            var statusEffect = statusEffectFactory?.Invoke();
            return statusEffect != null && (statusEffect.Categories & category) != 0;
        }

        public static int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender)
        {
            var adjustment = AbilityImpactEffects.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustment);
            adjustment += CombatDamageCalculator.GetRangedAttackDefenseIgnorePercentAdjustment(creature, skillType);

            var exposedOrSunderedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(
                Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType));
            if (AbilityImpactEffects.SkillTypeMatches(skillType, exposedOrSunderedSkillType) &&
                GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ExposedStatusEffect),
                    typeof(ExposeWeakPointStatusEffect),
                    typeof(SunderStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
            }

            if (GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ForceDisruptionStatusEffect),
                    typeof(FoggyMindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(
                    creature,
                    StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment);
            }

            return adjustment;
        }

    }
}

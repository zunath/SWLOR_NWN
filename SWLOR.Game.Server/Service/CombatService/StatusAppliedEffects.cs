using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class StatusAppliedEffects
    {
        internal static void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (!statusApplied)
                return;

            var requiredCategory = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedRequiredCategory));
            if (requiredCategory == 0 ||
                !WeaponStatusImpactEffects.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, requiredCategory))
            {
                return;
            }

            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilitySkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityDamageBonus);
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment);
            var window = Stat.GetStatAdjustment(activator, StatType.StatusAppliedNextSkillAbilityWindowSeconds);
            AbilityImpactEffects.GrantNextSkillAbilityBonuses(activator, skillType, damageBonus, criticalRate, window);

            StatusAppliedEffects.ApplyStatusAppliedSelfEffects(activator);
            StatusAppliedEffects.ApplyStatusAppliedTargetEffects(activator, target);
        }

        internal static void ApplyStatusAppliedSelfEffects(uint activator)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDurationSeconds);
            if (duration <= 0)
                return;

            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.AttackDeflection,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfAttackDeflection),
                duration,
                StatType.StatusAppliedSelfAttackDeflection);
            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfDefensePercentAdjustment);
            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.EvasionPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEvasionPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEvasionPercentAdjustment);
            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.ForceAttackPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfForceAttackPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfForceAttackPercentAdjustment);
            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.AttackDelayReductionPercent,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfHastePercentAdjustment),
                duration,
                StatType.StatusAppliedSelfHastePercentAdjustment);
            StatusAppliedEffects.ReplaceTemporaryStat(
                activator,
                StatType.EnmityPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfEnmityPercentAdjustment),
                duration,
                StatType.StatusAppliedSelfEnmityPercentAdjustment);

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.StatusAppliedSelfStaminaRestore);
            if (staminaRestore > 0)
                Stat.RestoreStamina(activator, staminaRestore);
        }

        internal static void ApplyStatusAppliedTargetEffects(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetDurationSeconds);
            if (duration <= 0)
                return;

            StatusAppliedEffects.ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment),
                duration,
                StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment);
            StatusAppliedEffects.ReplaceTemporaryStat(
                target,
                StatType.AccuracyPercentAdjustment,
                Stat.GetStatAdjustment(activator, StatType.StatusAppliedTargetAccuracyPercentAdjustment),
                duration,
                StatType.StatusAppliedTargetAccuracyPercentAdjustment);
        }

        internal static void ReplaceTemporaryStat(
            uint target,
            StatType statType,
            int amount,
            int durationSeconds,
            StatType group)
        {
            if (amount == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(target, statType, amount, durationSeconds, group);
        }

        internal static void ApplyAbilityTargetStatusEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility || !GetIsObjectValid(target))
                return;

            var requiredCategory = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusRequiredCategory));
            if (requiredCategory == 0 || !StatusEffect.HasStatusEffectCategory(target, requiredCategory))
                return;

            var physicalDefense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityTargetStatusDurationSeconds);
            StatusAppliedEffects.ReplaceTemporaryStat(
                target,
                StatType.PhysicalDefensePercentAdjustment,
                physicalDefense,
                duration,
                StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment);
        }

        internal static void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsAreaAbility || !GetIsObjectValid(target))
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceSkillType));
            var requiredCount = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.AreaAbilityTargetHitSequenceWindowSeconds);
            var exposedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds);
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) ||
                requiredCount <= 0 ||
                windowSeconds <= 0 ||
                exposedDuration <= 0)
            {
                return;
            }

            if (CombatState.TrackAreaAbilityTargetHitSequence(
                    activator,
                    target,
                    requiredCount,
                    windowSeconds))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }

        internal static void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType)
        {
            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.GuardedHitNextSkillAbilityStatusSkillType,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, storedSkillType))
                return;

            var duration = TemporaryStatModifier.Consume(
                activator,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds,
                StatType.GuardedHitNextSkillAbilityExposedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), duration, CombatDamageType.Physical);
        }

    }
}

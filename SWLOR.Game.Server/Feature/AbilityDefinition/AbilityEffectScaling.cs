using System;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class AbilityEffectScaling
    {
        private const int ScalingBaselineStat = 10;
        private const int ScalingCapStat = 26;
        private const float DirectEffectMaximumStatBonus = 0.25f;

        public static int ScaleDirectEffect(
            int baseAmount,
            int stat,
            float maximumBonusPercent = DirectEffectMaximumStatBonus,
            uint source = OBJECT_INVALID)
        {
            if (baseAmount <= 0 || maximumBonusPercent <= 0f)
                return ApplyActiveForceAffinityMagnitude(source, baseAmount);

            var progress = GetScalingProgress(stat);
            if (progress <= 0f)
                return ApplyActiveForceAffinityMagnitude(source, baseAmount);

            var bonus = (int)Math.Ceiling(baseAmount * maximumBonusPercent * progress);
            return ApplyActiveForceAffinityMagnitude(source, baseAmount + bonus);
        }

        public static int ApplyActiveForceAffinityMagnitude(uint source, int amount)
        {
            if (source == 0 || source == OBJECT_INVALID || !GetIsObjectValid(source) || amount <= 0)
                return amount;

            return Ability.ApplyActiveForceAffinityMagnitude(source, amount);
        }

        public static float ApplyActiveForceAffinityMagnitude(uint source, float amount)
        {
            if (source == 0 || source == OBJECT_INVALID || !GetIsObjectValid(source) || amount <= 0f)
                return amount;

            return Ability.ApplyActiveForceAffinityMagnitude(source, amount);
        }

        public static int ScaleValueBySourceSocial(uint source, int baseValue, int maximumValue)
        {
            if (baseValue >= maximumValue)
                return baseValue;

            var social = source != 0 && source != OBJECT_INVALID && GetIsObjectValid(source)
                ? GetAbilityScore(source, AbilityType.Social)
                : ScalingBaselineStat;

            var progress = GetScalingProgress(social);
            if (progress <= 0f)
                return baseValue;

            var bonusRange = maximumValue - baseValue;
            var bonus = (int)Math.Round(bonusRange * progress, MidpointRounding.AwayFromZero);
            return Math.Min(maximumValue, baseValue + bonus);
        }

        public static void ApplyScaledHeal(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        public static void ApplyActivatedScaledHeal(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        public static int CalculateScaledPercentOfMaxHP(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            if (!GetIsObjectValid(target))
                return 0;

            var baseAmount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(target) * (percent / 100f) * multiplier));
            var scalingSource = GetIsObjectValid(source) ? source : target;
            return ScaleDirectEffect(baseAmount, GetAbilityScore(scalingSource, scalingAbility), source: source);
        }

        public static void ApplyTemporaryHPPercent(
            uint source,
            uint target,
            string effectKey,
            float percent,
            float durationSeconds,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            if (GetIsObjectValid(source))
                amount = Ability.ApplyCombatReadinessMagnitude(source, amount);
            TemporaryHitPointEffects.ApplyFlat(target, effectKey, amount, durationSeconds);
        }

        private static float GetScalingProgress(int stat)
        {
            var scaledStat = Math.Clamp(stat, ScalingBaselineStat, ScalingCapStat);
            return (scaledStat - ScalingBaselineStat) / (float)(ScalingCapStat - ScalingBaselineStat);
        }
    }
}

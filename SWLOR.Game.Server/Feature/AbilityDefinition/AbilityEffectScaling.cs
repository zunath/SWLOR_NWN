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

        /// <summary>
        /// Heals and shields are multiplied by this at a scaling stat of 10 or below, rising in a straight line to
        /// <see cref="RestorativeMultiplierAtCap"/> at 26: a healer with 10 WIL heals half as much as one with 26.
        /// </summary>
        public const float RestorativeMultiplierAtBaseline = 0.625f;
        public const float RestorativeMultiplierAtCap = 1.25f;

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

        /// <summary>
        /// Scales a heal or shield by the healer's stat (see <see cref="RestorativeMultiplierAtBaseline"/>), then by the
        /// source's active Force affinity, which adds at most 25% to restorative effects.
        /// </summary>
        public static int ScaleRestorativeEffect(int baseAmount, int stat, uint source = OBJECT_INVALID)
        {
            if (baseAmount <= 0)
                return baseAmount;

            var multiplier = RestorativeMultiplierAtBaseline +
                             (RestorativeMultiplierAtCap - RestorativeMultiplierAtBaseline) * GetScalingProgress(stat);
            var scaled = baseAmount + (int)Math.Ceiling(baseAmount * (multiplier - 1f));
            return ApplyActiveForceAffinityRestorativeMagnitude(source, scaled);
        }

        public static int ApplyActiveForceAffinityRestorativeMagnitude(uint source, int amount)
        {
            if (source == 0 || source == OBJECT_INVALID || !GetIsObjectValid(source) || amount <= 0)
                return amount;

            return Ability.ApplyActiveForceAffinityRestorativeMagnitude(source, amount);
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

            var progress = GetSourceSocialProgress(source);
            if (progress <= 0f)
                return baseValue;

            var bonusRange = maximumValue - baseValue;
            var bonus = (int)Math.Round(bonusRange * progress, MidpointRounding.AwayFromZero);
            return Math.Min(maximumValue, baseValue + bonus);
        }

        /// <summary>A Leadership shield's size between its base and the size SOC 26 reaches.</summary>
        public static HealAmount ScaleHealBySourceSocial(uint source, HealAmount minimum, HealAmount maximum)
        {
            return HealAmount.Between(minimum, maximum, GetSourceSocialProgress(source));
        }

        private static float GetSourceSocialProgress(uint source)
        {
            var social = source != 0 && source != OBJECT_INVALID && GetIsObjectValid(source)
                ? GetAbilityScore(source, AbilityType.Social)
                : ScalingBaselineStat;

            return GetScalingProgress(social);
        }

        public static void ApplyScaledHeal(
            uint source,
            uint target,
            HealAmount heal,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledHeal(source, target, heal, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        public static void ApplyActivatedScaledHeal(
            uint source,
            uint target,
            HealAmount heal,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledHeal(source, target, heal, scalingAbility, multiplier);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(source, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount, isInstantAbilityHeal: true);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        public static int CalculateScaledHeal(
            uint source,
            uint target,
            HealAmount heal,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            if (!GetIsObjectValid(target))
                return 0;

            var baseAmount = heal.ForMaxHitPoints(GetMaxHitPoints(target), multiplier);
            var scalingSource = GetIsObjectValid(source) ? source : target;
            return ScaleRestorativeEffect(baseAmount, GetAbilityScore(scalingSource, scalingAbility), source);
        }

        public static void ApplyTemporaryHP(
            uint source,
            uint target,
            string effectKey,
            HealAmount shield,
            float durationSeconds,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = CalculateScaledHeal(source, target, shield, scalingAbility, multiplier);
            if (GetIsObjectValid(source))
                amount = Ability.ApplyCombatReadinessMagnitude(source, amount);
            TemporaryHitPointEffects.ApplyFlatFromSource(source, target, effectKey, amount, durationSeconds);
        }

        private static float GetScalingProgress(int stat)
        {
            var scaledStat = Math.Clamp(stat, ScalingBaselineStat, ScalingCapStat);
            return (scaledStat - ScalingBaselineStat) / (float)(ScalingCapStat - ScalingBaselineStat);
        }
    }
}

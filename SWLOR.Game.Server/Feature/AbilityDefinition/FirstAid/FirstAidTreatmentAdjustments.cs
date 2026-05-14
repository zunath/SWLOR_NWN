using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public static class FirstAidTreatmentAdjustments
    {
        public static void ApplyMedicalScaledHeal(
            uint source,
            uint target,
            float percent,
            AbilityType scalingAbility = AbilityType.Willpower,
            float multiplier = 1f)
        {
            var amount = AbilityEffectScaling.CalculateScaledPercentOfMaxHP(source, target, percent, scalingAbility, multiplier);
            amount = ApplyMedicalHealingBonus(source, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        public static int ApplyMedicalHealingBonus(uint source, int amount)
        {
            if (amount <= 0 || !GetIsObjectValid(source))
                return amount;

            var adjustment = Stat.GetStatAdjustment(source, StatType.FirstAidMedicalHealingPercentAdjustment);
            if (adjustment <= 0)
                return amount;

            return amount + (int)Math.Ceiling(amount * (adjustment / 100f));
        }

        public static float ApplyStimDurationBonus(uint source, float durationSeconds)
        {
            if (durationSeconds <= 0f || !GetIsObjectValid(source))
                return durationSeconds;

            var adjustment = Stat.GetStatAdjustment(source, StatType.StimPackDurationPercentAdjustment);
            if (adjustment <= 0)
                return durationSeconds;

            return durationSeconds + durationSeconds * (adjustment / 100f);
        }
    }
}

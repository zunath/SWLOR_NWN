using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    internal static class ForcePressureEffects
    {
        public static void ApplyUnstablePressure(uint activator, uint target)
        {
            var evasionPenalty = Stat.GetStatAdjustment(activator, StatType.SparkLightningPressureEvasionPenaltyPercent);
            var durationSeconds = Stat.GetStatAdjustment(activator, StatType.SparkLightningPressureDurationSeconds);
            if (evasionPenalty <= 0 || durationSeconds <= 0)
                return;

            var lowHPForceDamageTaken = Stat.GetStatAdjustment(activator, StatType.SparkLightningPressureLowHPForceDamageTakenPercent);
            var lowHPThreshold = Stat.GetStatAdjustment(activator, StatType.SparkLightningPressureLowHPThresholdPercent);
            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new UnstablePressureStatusEffect(evasionPenalty, lowHPForceDamageTaken, lowHPThreshold),
                durationSeconds,
                CombatDamageType.Force);
        }
    }
}

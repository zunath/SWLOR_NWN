using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class HitPointSpendEffects
    {
        public static void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent = 0)
        {
            var window = Math.Max(1, Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds));

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseSoulAscension) > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.HeavyVibrobladeOffenseSoulAscension,
                    1,
                    window,
                    StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            }

            HitPointSpendEffects.ApplyHitPointSpendStaminaRestore(activator);
            HitPointSpendEffects.ApplyHitPointSpendTemporaryHitPoints(activator, hitPointsSpent);
        }

        internal static void ApplyHitPointSpendTemporaryHitPoints(uint activator, int hitPointsSpent)
        {
            if (hitPointsSpent <= 0)
                return;

            var percent = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            var duration = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPDurationSeconds);
            if (percent <= 0 || duration <= 0)
                return;

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(hitPointsSpent * (percent / 100f)));
            ApplyEffectToObject(
                DurationType.Temporary,
                EffectTemporaryHitpoints(temporaryHP),
                activator,
                duration);
        }

        internal static void ApplyHitPointSpendStaminaRestore(uint activator)
        {
            var basePercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (basePercent <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = AbilityImpactEffects.GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximum = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
            {
                percent = Math.Min(maximum, percent);
            }

            var stamina = Math.Max(1, (int)Math.Ceiling(Stat.GetMaxStamina(activator) * (percent / 100f)));
            Stat.RestoreStamina(activator, stamina);
        }

        internal static void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability)
        {
            if (QueuedCombatActions.GetAbilitySkillType(activator, ability) != SkillType.Force ||
                !ability.Requirements.OfType<AbilityRequirementFP>().Any(x => x.RequiredFP > 0))
            {
                return;
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForcePrecognition) > 0 &&
                CombatStatTriggers.TryUseStatTrigger(activator, StatType.ForcePrecognition, 12))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(PrecognitionStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForceConvergence) > 0 &&
                CombatStatTriggers.TryUseStatTrigger(activator, StatType.ForceConvergence, 45))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(ForceConvergenceStatusEffect), 30f);
            }
        }

        internal static void ApplyAbilityUsedMasterAbilityHitChance(uint activator)
        {
            var master = GetMaster(activator);
            if (!GetIsObjectValid(master))
                return;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds);
            if (adjustment == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                master,
                StatType.AbilityHitChancePercentAdjustment,
                adjustment,
                duration,
                StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
        }
    }
}

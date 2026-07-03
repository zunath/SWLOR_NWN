using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AttackConditionBonuses
    {
        public static int PrepareOpeningAutoAttack(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var idleSeconds = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackIdleSeconds);
            if (idleSeconds <= 0)
                return 0;

            if (CombatActivity.HasRecentCombatActivity(attacker, idleSeconds))
                return 0;

            CombatActivity.TrackCombatActivity(attacker);

            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackDamageBonus);
            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackDamageBonus,
                    damageBonus,
                    6,
                    StatType.CurrentAutoAttackDamageBonus);
            }

            return Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment);
        }

        internal static int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return 0;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleCriticalRateSkillType));
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleCriticalRateRequiredCount);
            var criticalRate = Stat.GetStatAdjustment(attacker, StatType.AutoAttackCycleCriticalRatePercentAdjustment);
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) || requiredCount <= 0 || criticalRate <= 0)
                return 0;

            if (!CombatState.IncrementAutoAttackCriticalCycle(attacker, requiredCount))
                return 0;

            return criticalRate;
        }

        internal static int GetLowHPCriticalRateAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPCriticalRatePercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        public static int GetLowHPAttackAdjustment(uint attacker)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.LowHPAttackPercentAdjustment);
            var maximumHP = GetMaxHitPoints(attacker);
            if (threshold <= 0 || adjustment == 0 || maximumHP <= 0)
                return 0;

            return GetCurrentHitPoints(attacker) <= maximumHP * (threshold / 100f)
                ? adjustment
                : 0;
        }

        internal static int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var category = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetStatusCriticalRateStatusCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetStatusCriticalRatePercentAdjustment);
            if (category == 0 || adjustment == 0 || !WeaponAbilityImpactEffects.TargetHasAnyStatusEffectCategory(defender, category))
                return 0;

            return adjustment;
        }

    }
}

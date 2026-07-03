using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityRecoveryEffects
    {
        public static void ApplyAbilityRestoredFPEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredFPHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                haste,
                duration,
                StatType.AbilityRestoredFPHastePercentAdjustment);
        }

        public static void ApplyAbilityRestoredBothResourcesEffects(uint activator)
        {
            var haste = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityRestoredBothResourcesHasteDurationSeconds);
            if (haste == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDelayReductionPercent,
                haste,
                duration,
                StatType.AbilityRestoredBothResourcesHastePercentAdjustment);
        }

        public static bool HasRecentCombatAbilityUse(uint activator, float windowSeconds)
        {
            return CombatState.HasRecentCombatAbilityUse(activator, windowSeconds);
        }

        public static bool IsUsingAbility(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (Activity.GetBusyType(creature) == ActivityStatusType.AbilityActivation ||
                    HasRecentCombatAbilityUse(creature, 2f));
        }

        public static (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return (0, 0, 0);

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilitySkillType));
            var requiredIdleSeconds = Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityRequiredIdleSeconds);
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) || requiredIdleSeconds <= 0)
                return (0, 0, 0);

            if (CombatState.HasRecentCombatAbilityUse(activator, requiredIdleSeconds))
                return (0, 0, 0);

            return (
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityDamageBonus),
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityHitChancePercentAdjustment),
                Stat.GetStatAdjustment(activator, StatType.IdleSkillAbilityCriticalDamagePercentAdjustment));
        }

        internal static int GetIdleAbilityHitChanceAdjustment(uint activator, SkillType skillType)
        {
            return AbilityRecoveryEffects.GetIdleSkillAbilityBonuses(activator, skillType).HitChancePercentAdjustment;
        }

        internal static void ApplyNextAutoAttackDamageBonus(
            uint activator,
            StatType bonusStatType,
            StatType durationStatType)
        {
            var bonus = Stat.GetStatAdjustment(activator, bonusStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (bonus == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.NextAutoAttackDamageBonus,
                bonus,
                duration,
                StatType.NextAutoAttackDamageBonus);
        }

        internal static void ApplyAbilityUsedEvasion(
            uint activator,
            StatType evasionStatType,
            StatType durationStatType,
            StatType targetStatType = StatType.EvasionPercentAdjustment)
        {
            var evasionPercent = Stat.GetStatAdjustment(activator, evasionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                targetStatType,
                evasionPercent,
                duration,
                targetStatType);
        }

        internal static void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid)
        {
            var attackDeflection = Stat.GetStatAdjustment(activator, attackDeflectionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (attackDeflection == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDeflection,
                attackDeflection,
                duration,
                StatType.AttackDeflection);
            if (deflectionFPRestoreStatType != StatType.Invalid)
            {
                var fpRestore = Stat.GetStatAdjustment(activator, deflectionFPRestoreStatType);
                if (fpRestore > 0)
                {
                    TemporaryStatModifier.Replace(
                        activator,
                        StatType.DeflectionFPRestore,
                        fpRestore,
                        duration,
                        deflectionFPRestoreStatType);
                }
            }

            AbilityGrantedDeflectionEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator);
        }

    }
}

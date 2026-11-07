using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    // The Suppression stacks live in Domain/Combat/CombatSuppressionService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatSuppressionService Suppression => FacadeAccessor.Resolve<ICombatSuppressionService>();

        private static void ApplyAutoAttackSuppressionStack(uint attacker, uint defender, SkillType skillType, CombatDamageType damageType) =>
            Suppression.ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);

        private static void ApplyRangedHitSuppressionStack(uint attacker, uint defender, SkillType skillType, CombatDamageType damageType) =>
            Suppression.ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);

        public static bool ApplySuppressionStack(
            uint attacker,
            uint defender,
            int evasionPenaltyPercent,
            int durationSeconds,
            CombatDamageType damageType,
            int evasionPenaltyAdjustment = 0) =>
            Suppression.ApplySuppressionStack(attacker, defender, evasionPenaltyPercent, durationSeconds, damageType, evasionPenaltyAdjustment);

        public static int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID) => Suppression.GetSuppressionStackCount(target, source);

        public static void ReconcileContainmentNetStatus(uint source, uint target) => Suppression.ReconcileContainmentNetStatus(source, target);

        public static void ReconcileContainmentNetStatuses(uint target) => Suppression.ReconcileContainmentNetStatuses(target);

        public static int ConsumeSuppressionRangedAttackAccuracyAdjustment(uint attacker, uint defender, SkillType skillType) =>
            Suppression.ConsumeSuppressionRangedAttackAccuracyAdjustment(attacker, defender, skillType);

        public static int GetRangedAbilityLongRangeHitChanceAdjustment(uint creature, uint defender, SkillType skillType) =>
            Suppression.GetRangedAbilityLongRangeHitChanceAdjustment(creature, defender, skillType);

        public static int GetRangedAbilityLongRangeCriticalRateAdjustment(uint creature, uint defender, SkillType skillType) =>
            Suppression.GetRangedAbilityLongRangeCriticalRateAdjustment(creature, defender, skillType);

        public static void ReconcileOverwatchStatus(uint source) => Suppression.ReconcileOverwatchStatus(source);

        private static void TrackSuppressionAbilityUse(uint activator, DateTime usedAt) => Suppression.TrackSuppressionAbilityUse(activator, usedAt);

        private static void ForgetSuppressionState(uint creature) => Suppression.ForgetSuppressionState(creature);
    }
}

using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Suppression stacks for combat: applying them, the Containment Net statuses they drive and the Overwatch window
    /// that a suppressed creature's ability use opens.
    /// </summary>
    public interface ICombatSuppressionService
    {
        void ApplyAutoAttackSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType);

        void ApplyRangedHitSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType);

        bool ApplySuppressionStack(
            uint attacker,
            uint defender,
            int evasionPenaltyPercent,
            int durationSeconds,
            CombatDamageType damageType,
            int evasionPenaltyAdjustment = 0);

        int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID);

        void ReconcileContainmentNetStatus(uint source, uint target);

        void ReconcileContainmentNetStatuses(uint target);

        int ConsumeSuppressionRangedAttackAccuracyAdjustment(uint attacker, uint defender, SkillType skillType);

        int GetRangedAbilityLongRangeHitChanceAdjustment(
            uint creature,
            uint defender,
            SkillType skillType);

        int GetRangedAbilityLongRangeCriticalRateAdjustment(
            uint creature,
            uint defender,
            SkillType skillType);

        void ReconcileOverwatchStatus(uint source);

        void TrackSuppressionAbilityUse(uint activator, DateTime usedAt);

        void ForgetSuppressionState(uint creature);
    }
}

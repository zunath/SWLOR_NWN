using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Combat activity records: when a creature last attacked, used a hostile ability, took or dealt damage and entered
    /// combat, and the First Strike stacks, stealth opening window and idle-readiness state those records drive.
    /// </summary>
    public interface ICombatActivityService
    {
        /// <summary>
        /// The last time each attacker damaged each defender, keyed (attacker, defender): the instance's own table, which
        /// the hit effects part of <c>Combat</c> reads and prunes and the damage taken part reads.
        /// </summary>
        Dictionary<(uint, uint), DateTime> RecentDamageTargets { get; }

        void ApplyFirstCombatAttackStaminaRestore(uint attacker);

        void PurgeInvalidOffensiveActivity();

        void TrackRecentDamageTarget(uint attacker, uint defender);

        void TrackRecentDamageTaken(uint creature);

        bool HasRecentDamageTaken(uint creature, float windowSeconds);

        void TrackCombatActivity(uint creature);

        void TrackAttackActivity(uint creature);

        void TrackHostileAbilityActivity(uint creature, bool countAsCompletedOffensiveActivity = false);

        void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker);

        bool HasRecentHostileCombatActivity(uint creature);

        void TrackStealthOpeningWindow(uint creature);

        bool HasRecentAttackActivity(uint creature, float windowSeconds);

        DateTime GetLastOffensiveActivityAt(uint creature);

        DateTime GetLastCompletedOffensiveActivityAt(uint creature);

        bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds);

        int GetDirectDamageToStatusCategoryOrStealthBonus(uint attacker, uint defender);

        int GetFirstHostileAbilityHitDamageBonus(uint attacker, AbilityDetail ability);

        void ApplyFirstHostileAbilityHitCount(uint attacker, AbilityDetail ability);

        void TrackCombatAbilityUse(uint activator, AbilityDetail ability);

        bool HasRecentCombatAbilityUse(uint activator, float windowSeconds);

        bool IsUsingAbility(uint creature);

        (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(uint activator, SkillType skillType);

        void StoreAbilityActivationIdleBonuses(uint activator, SkillType skillType);

        void ClearAbilityActivationIdleBonuses(uint activator);

        void RestoreIdleReadinessOnPlayerEnter();

        void ScheduleIdleReadinessRefresh(uint creature);

        bool HasConfiguredIdleSkillAbilityChannel(uint creature);

        int GetIdleAbilityHitChanceAdjustment(uint activator, SkillType skillType);

        void ForgetActivityState(uint creature);

        void ForgetOpeningActivityState(uint creature);
    }
}

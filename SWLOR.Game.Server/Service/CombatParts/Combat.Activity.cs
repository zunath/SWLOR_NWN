using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    // The combat activity records live in Domain/Combat/CombatActivityService.cs. These forwarders keep every member on Combat for its
    // callers, and the two handlers stay here under their names.
    public static partial class Combat
    {
        private static ICombatActivityService CombatActivity => FacadeAccessor.Resolve<ICombatActivityService>();

        private static void ApplyFirstCombatAttackStaminaRestore(uint attacker) => CombatActivity.ApplyFirstCombatAttackStaminaRestore(attacker);

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void PurgeInvalidOffensiveActivity() => CombatActivity.PurgeInvalidOffensiveActivity();

        private static void TrackRecentDamageTarget(uint attacker, uint defender) => CombatActivity.TrackRecentDamageTarget(attacker, defender);

        private static void TrackRecentDamageTaken(uint creature) => CombatActivity.TrackRecentDamageTaken(creature);

        public static bool HasRecentDamageTaken(uint creature, float windowSeconds) => CombatActivity.HasRecentDamageTaken(creature, windowSeconds);

        private static void TrackCombatActivity(uint creature) => CombatActivity.TrackCombatActivity(creature);

        public static void TrackAttackActivity(uint creature) => CombatActivity.TrackAttackActivity(creature);

        public static void TrackHostileAbilityActivity(uint creature, bool countAsCompletedOffensiveActivity = false) =>
            CombatActivity.TrackHostileAbilityActivity(creature, countAsCompletedOffensiveActivity);

        public static void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) =>
            CombatActivity.TrackHostileDefensiveCombatEntryActivity(creature, attacker);

        public static bool HasRecentHostileCombatActivity(uint creature) => CombatActivity.HasRecentHostileCombatActivity(creature);

        public static void TrackStealthOpeningWindow(uint creature) => CombatActivity.TrackStealthOpeningWindow(creature);

        public static bool HasRecentAttackActivity(uint creature, float windowSeconds) =>
            CombatActivity.HasRecentAttackActivity(creature, windowSeconds);

        public static DateTime GetLastOffensiveActivityAt(uint creature) => CombatActivity.GetLastOffensiveActivityAt(creature);

        public static DateTime GetLastCompletedOffensiveActivityAt(uint creature) => CombatActivity.GetLastCompletedOffensiveActivityAt(creature);

        public static bool HasRecentDamageTarget(uint attacker, uint defender, float windowSeconds) =>
            CombatActivity.HasRecentDamageTarget(attacker, defender, windowSeconds);

        private static int GetDirectDamageToStatusCategoryOrStealthBonus(uint attacker, uint defender) =>
            CombatActivity.GetDirectDamageToStatusCategoryOrStealthBonus(attacker, defender);

        private static int GetFirstHostileAbilityHitDamageBonus(uint attacker, AbilityDetail ability) =>
            CombatActivity.GetFirstHostileAbilityHitDamageBonus(attacker, ability);

        private static void ApplyFirstHostileAbilityHitCount(uint attacker, AbilityDetail ability) =>
            CombatActivity.ApplyFirstHostileAbilityHitCount(attacker, ability);

        private static void TrackCombatAbilityUse(uint activator, AbilityDetail ability) => CombatActivity.TrackCombatAbilityUse(activator, ability);

        public static bool HasRecentCombatAbilityUse(uint activator, float windowSeconds) =>
            CombatActivity.HasRecentCombatAbilityUse(activator, windowSeconds);

        public static bool IsUsingAbility(uint creature) => CombatActivity.IsUsingAbility(creature);

        public static (int DamageBonus, int HitChancePercentAdjustment, int CriticalDamagePercentAdjustment) GetIdleSkillAbilityBonuses(
            uint activator,
            SkillType skillType) =>
            CombatActivity.GetIdleSkillAbilityBonuses(activator, skillType);

        public static void StoreAbilityActivationIdleBonuses(uint activator, SkillType skillType) =>
            CombatActivity.StoreAbilityActivationIdleBonuses(activator, skillType);

        public static void ClearAbilityActivationIdleBonuses(uint activator) => CombatActivity.ClearAbilityActivationIdleBonuses(activator);

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RestoreIdleReadinessOnPlayerEnter() => CombatActivity.RestoreIdleReadinessOnPlayerEnter();

        private static void ScheduleIdleReadinessRefresh(uint creature) => CombatActivity.ScheduleIdleReadinessRefresh(creature);

        private static bool HasConfiguredIdleSkillAbilityChannel(uint creature) => CombatActivity.HasConfiguredIdleSkillAbilityChannel(creature);

        private static int GetIdleAbilityHitChanceAdjustment(uint activator, SkillType skillType) =>
            CombatActivity.GetIdleAbilityHitChanceAdjustment(activator, skillType);

        private static void ForgetActivityState(uint creature) => CombatActivity.ForgetActivityState(creature);

        private static void ForgetOpeningActivityState(uint creature) => CombatActivity.ForgetOpeningActivityState(creature);
    }
}

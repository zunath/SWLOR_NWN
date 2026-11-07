using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    // The attack timing lives in Domain/Combat/CombatAttackTimingService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatAttackTimingService AttackTiming => FacadeAccessor.Resolve<ICombatAttackTimingService>();

        /// <inheritdoc cref="ICombatAttackTimingService.ConsumeNextAbilityDelayReductionPercent"/>
        public static int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability) =>
            AttackTiming.ConsumeNextAbilityDelayReductionPercent(creature, ability);

        public static bool HasNextAutoAttackNoDelay(uint creature, SkillType skillType) => AttackTiming.HasNextAutoAttackNoDelay(creature, skillType);

        public static bool HasTemporaryNextAutoAttackNoDelay(uint creature, SkillType skillType) =>
            AttackTiming.HasTemporaryNextAutoAttackNoDelay(creature, skillType);

        public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType) =>
            AttackTiming.ConsumeNextAutoAttackNoDelay(creature, skillType);

        public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType) =>
            AttackTiming.ConsumeNextAutoAttackCriticalRateBonus(creature, skillType);

        public static bool IsAttackDelayReductionSuppressed(uint creature) => AttackTiming.IsAttackDelayReductionSuppressed(creature);

        public static void GrantNextAutoAttackNoDelay(uint creature, SkillType skillType, int durationSeconds) =>
            AttackTiming.GrantNextAutoAttackNoDelay(creature, skillType, durationSeconds);

        public static void GrantNextAutoAttackNoDelay(uint creature, int durationSeconds) =>
            AttackTiming.GrantNextAutoAttackNoDelay(creature, durationSeconds);

        public static void GrantNextAutoAttackCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment,
            int durationSeconds) =>
            AttackTiming.GrantNextAutoAttackCriticalRateBonus(creature, skillType, criticalRatePercentAdjustment, durationSeconds);

        public static void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds) =>
            AttackTiming.GrantNextAbilityNoDelay(creature, skillTypeValue, durationSeconds);

        /// <inheritdoc cref="ICombatAttackTimingService.GrantNextAbilityNoDelay(uint, SkillType, int, int)"/>
        public static void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds, int delayReductionPercent = 0) =>
            AttackTiming.GrantNextAbilityNoDelay(creature, skillType, durationSeconds, delayReductionPercent);

        private static void GrantNextAbilityDamageBonus(uint creature, PerkType perkType, int bonus, int durationSeconds) =>
            AttackTiming.GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds,
            int defenseIgnorePercentAdjustment = 0,
            StatusEffectCategory requiredCategory = StatusEffectCategory.None) =>
            AttackTiming.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRatePercentAdjustment, durationSeconds, defenseIgnorePercentAdjustment, requiredCategory);

        private static void GrantStatusAppliedNextAttackDamageBonus(uint creature, int damageBonus, int durationSeconds) =>
            AttackTiming.GrantStatusAppliedNextAttackDamageBonus(creature, damageBonus, durationSeconds);

        private static void GrantCuedNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false) =>
            AttackTiming.GrantCuedNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, durationSeconds, group, rangedOnly, hostileOnly);

        private static void ShowReadyCue(uint creature, bool wasArmed, int bonus, string text) =>
            AttackTiming.ShowReadyCue(creature, wasArmed, bonus, text);

        public static void GrantNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false) =>
            AttackTiming.GrantNextSkillAbilityStaminaCostAdjustment(creature, skillType, adjustment, durationSeconds, group, rangedOnly, hostileOnly);

        public static void ApplyBleedingStatusExpiredEffects(uint source) => AttackTiming.ApplyBleedingStatusExpiredEffects(source);

        private static void GrantNextSkillAutoAttackDamageBonus(uint creature, SkillType skillType, int damageBonus, int durationSeconds) =>
            AttackTiming.GrantNextSkillAutoAttackDamageBonus(creature, skillType, damageBonus, durationSeconds);

        private static void GrantNextAbilityFPCostAdjustment(uint creature, SkillType skillType, int adjustment, int durationSeconds) =>
            AttackTiming.GrantNextAbilityFPCostAdjustment(creature, skillType, adjustment, durationSeconds);

        /// <inheritdoc cref="ICombatAttackTimingService.CalculateAttackDelay(uint)"/>
        public static int CalculateAttackDelay(uint attacker) => AttackTiming.CalculateAttackDelay(attacker);

        /// <inheritdoc cref="ICombatAttackTimingService.CalculateAttackDelay(uint, int)"/>
        public static int CalculateAttackDelay(uint attacker, int attackDelayReductionAdjustment) =>
            AttackTiming.CalculateAttackDelay(attacker, attackDelayReductionAdjustment);

        /// <inheritdoc cref="ICombatAttackTimingService.ConsumeAttacksPerSwing(uint, int)"/>
        public static int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds) =>
            AttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelayMilliseconds);

        /// <inheritdoc cref="ICombatAttackTimingService.ConsumeAttacksPerSwing(uint, int, int, bool)"/>
        public static int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds, int unbuffedDelayMilliseconds, bool hasNoDelayBuff) =>
            AttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelayMilliseconds, unbuffedDelayMilliseconds, hasNoDelayBuff);

        /// <inheritdoc cref="ICombatAttackTimingService.ConsumeAttacksPerSwing(uint, int, int, bool, int, int, int, int, Nullable{LimitedAttackTimingBudget}, Nullable{LimitedAttackTimingBudget}, LimitedAttackTimingBudget)"/>
        public static int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff,
            int effectiveDelayWithoutLimitedReductionMilliseconds,
            int limitedReductionRemainingAttacks,
            int limitedNoDelayRemainingAttacks = 0,
            int attacksPerCycle = 1,
            LimitedAttackTimingBudget? limitedReductionBudget = null,
            LimitedAttackTimingBudget? limitedNoDelayBudget = null,
            LimitedAttackTimingBudget temporaryNoDelayBudget = default) =>
            AttackTiming.ConsumeAttacksPerSwing(attacker, effectiveDelayMilliseconds, unbuffedDelayMilliseconds, hasNoDelayBuff, effectiveDelayWithoutLimitedReductionMilliseconds, limitedReductionRemainingAttacks, limitedNoDelayRemainingAttacks, attacksPerCycle, limitedReductionBudget, limitedNoDelayBudget, temporaryNoDelayBudget);

        /// <inheritdoc cref="ICombatAttackTimingService.ClearAttackSwingDebt"/>
        public static void ClearAttackSwingDebt(uint attacker) => AttackTiming.ClearAttackSwingDebt(attacker);

        /// <inheritdoc cref="ICombatAttackTimingService.HandleParalyze"/>
        public static bool HandleParalyze(uint attacker) => AttackTiming.HandleParalyze(attacker);

        /// <inheritdoc cref="ICombatAttackTimingService.CalculateAttackDelayReduction"/>
        public static int CalculateAttackDelayReduction(uint attacker) => AttackTiming.CalculateAttackDelayReduction(attacker);

        /// <inheritdoc cref="ICombatAttackTimingService.GetTargetAttackDelayReduction"/>
        public static int GetTargetAttackDelayReduction(uint attacker, uint target, SkillType skillType) =>
            AttackTiming.GetTargetAttackDelayReduction(attacker, target, skillType);

        public static int CalculateOffhandAttackDelayReduction(uint attacker) => AttackTiming.CalculateOffhandAttackDelayReduction(attacker);

        /// <inheritdoc cref="ICombatAttackTimingService.GetWeaponDelay"/>
        public static int GetWeaponDelay(uint item) => AttackTiming.GetWeaponDelay(item);

        private static void ForgetAttackTimingState(uint creature) => AttackTiming.ForgetAttackTimingState(creature);
    }
}

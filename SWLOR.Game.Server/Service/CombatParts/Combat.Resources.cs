using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    // The resource bookkeeping lives in Domain/Combat/CombatResourcesService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        private static ICombatResourcesService Resources => FacadeAccessor.Resolve<ICombatResourcesService>();
        public static IDisposable BeginDamageDerivedHealing(uint creature, uint target) => Resources.BeginDamageDerivedHealing(creature, target);

        /// <inheritdoc cref="ICombatResourcesService.RefreshDamageDerivedHealingTargetHP"/>
        private static void RefreshDamageDerivedHealingTargetHP(uint creature, uint target) =>
            Resources.RefreshDamageDerivedHealingTargetHP(creature, target);

        public static int ApplyDamageDerivedHealing(uint creature, int damage, int percent, bool isActivatedHealing = false, int passiveHealingCooldownSeconds = 0) =>
            Resources.ApplyDamageDerivedHealing(creature, damage, percent, isActivatedHealing, passiveHealingCooldownSeconds);

        private static bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent) =>
            Resources.DidCrossHPThreshold(creature, damage, thresholdPercent);

        private static void HealFromMaxHP(uint creature, int percent) => Resources.HealFromMaxHP(creature, percent);

        internal static bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds) =>
            Resources.TryUseStatTrigger(creature, statType, cooldownSeconds);

        internal static bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown) =>
            Resources.TryUseStatTrigger(creature, statType, cooldown);

        private static int GetHighResourceStatAdjustment(uint activator, StatType payload, StatType threshold, AbilityDetail ability) =>
            Resources.GetHighResourceStatAdjustment(activator, payload, threshold, ability);

        private static int GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability) =>
            Resources.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(creature, ability);

        private static int RestoreFPFromStat(uint creature, StatType trigger, int amount) => Resources.RestoreFPFromStat(creature, trigger, amount);

        private static int RestoreStaminaFromStat(uint creature, StatType trigger, int amount) =>
            Resources.RestoreStaminaFromStat(creature, trigger, amount);

        private static void ReduceFPFromStat(uint creature, StatType trigger, int amount) => Resources.ReduceFPFromStat(creature, trigger, amount);

        private static void ReduceStaminaFromStat(uint creature, StatType trigger, int amount) =>
            Resources.ReduceStaminaFromStat(creature, trigger, amount);

        private static int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested) =>
            Resources.RestoreAbilityHitStaminaFromStat(creature, trigger, requested);

        private static int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested, AbilityDetail ability) =>
            Resources.RestoreAbilityHitStaminaFromStat(creature, trigger, requested, ability);

        public static int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability) =>
            Resources.RefundCriticalRangedAbilityStaminaCost(creature, ability);

        public static void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost) =>
            Resources.ApplyAbilityStaminaCostFPRestore(creature, ability, staminaCost);

        private static bool TryGetAbilityStaminaCostState(uint creature, AbilityDetail ability, out CombatAbilityStaminaCostState state) =>
            Resources.TryGetAbilityStaminaCostState(creature, ability, out state);

        /// <inheritdoc cref="ICombatResourcesService.GetAbilityStaminaPaid"/>
        public static int GetAbilityStaminaPaid(uint creature, AbilityDetail ability) => Resources.GetAbilityStaminaPaid(creature, ability);

        /// <inheritdoc cref="ICombatResourcesService.RestoreAbilityHitStamina"/>
        public static int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool sendFeedback = true, bool retainMinimumCost = true) =>
            Resources.RestoreAbilityHitStamina(creature, ability, requested, sendFeedback, retainMinimumCost);

        public static int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability) => Resources.GetPaidAbilityStaminaCost(creature, ability);

        public static void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
            Resources.DeferAbilityStaminaCostContext(creature, ability);

        public static void CompleteAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
            Resources.CompleteAbilityStaminaCostContext(creature, ability);

        public static void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability) =>
            Resources.CompleteDeferredAbilityStaminaCostContext(creature, ability);

        public static void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost) =>
            Resources.ApplyAbilityFPCostStaminaRestore(creature, ability, fpCost);

        public static int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType) =>
            Resources.GetNextAbilityFPCostAdjustment(creature, skillType);

        public static int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType) =>
            Resources.ConsumeNextAbilityFPCostAdjustment(creature, skillType);

        public static void ApplyAbilityRestoredFPEffects(uint activator) => Resources.ApplyAbilityRestoredFPEffects(activator);

        public static void ApplyFPRestoredEffects(uint creature) => Resources.ApplyFPRestoredEffects(creature);

        public static void ApplyStaminaRestoredEffects(uint creature) => Resources.ApplyStaminaRestoredEffects(creature);

        public static void ApplyAbilityRestoredBothResourcesEffects(uint activator) => Resources.ApplyAbilityRestoredBothResourcesEffects(activator);

        private static bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent) =>
            Resources.IsCurrentFPAtOrAbovePercent(creature, thresholdPercent);

        public static bool IsCurrentFPAndStaminaAbovePercent(uint creature, int thresholdPercent, AbilityDetail ability = null) =>
            Resources.IsCurrentFPAndStaminaAbovePercent(creature, thresholdPercent, ability);

        public static bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent) =>
            Resources.IsCurrentFPAndStaminaAtOrBelowPercent(creature, thresholdPercent);

        private static void ForgetResourceTriggerState(uint creature) => Resources.ForgetResourceTriggerState(creature);

        private static void ForgetAbilityStaminaCostState(uint creature) => Resources.ForgetAbilityStaminaCostState(creature);
    }
}

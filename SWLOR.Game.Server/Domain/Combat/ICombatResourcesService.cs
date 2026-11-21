using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// FP, stamina and healing bookkeeping for combat: stat trigger cooldowns, damage-derived healing and its budgets, the
    /// stamina a hostile ability paid and what its hits may refund, and the healing adjustments and what a received heal
    /// sets off (part 7 of the stat split; until then members of <c>IStatService</c>). The riders that restored FP or
    /// stamina grant are <c>IResourceRestoredEffectsService</c>'s (part 8).
    /// </summary>
    public interface ICombatResourcesService
    {
        IDisposable BeginDamageDerivedHealing(uint creature, uint target);

        /// <summary>
        /// Includes defensive HP granted during damage calculation before critical healing
        /// or queued damage can consume the hit's snapshot. Started healing remains frozen.
        /// </summary>
        void RefreshDamageDerivedHealingTargetHP(uint creature, uint target);

        int ApplyDamageDerivedHealing(
            uint creature,
            int damage,
            int percent,
            bool isActivatedHealing = false,
            int passiveHealingCooldownSeconds = 0);

        bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent);

        void HealFromMaxHP(uint creature, int percent);

        int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount);

        int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment);

        /// <param name="isInstantAbilityHeal">True for a heal an ability lands when it resolves. Only these can restore Stamina through the healing-received trigger.</param>
        int ApplyHealingReceivedAdjustment(uint creature, int amount, bool applyReceivedEffects = true, bool isInstantAbilityHeal = false);

        int CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP);

        /// <param name="isInstantAbilityHeal">Heals over time, lifesteal, drains and regeneration pass false and never restore Stamina.</param>
        void ApplyHealingReceivedEffects(uint creature, int amount, bool isInstantAbilityHeal = false);

        bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds);

        bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown);

        int GetHighResourceStatAdjustment(uint activator, StatType payload, StatType threshold, AbilityDetail ability);

        int GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(
            uint creature,
            AbilityDetail ability);

        int RestoreFPFromStat(uint creature, StatType trigger, int amount);

        int RestoreStaminaFromStat(uint creature, StatType trigger, int amount);

        void ReduceFPFromStat(uint creature, StatType trigger, int amount);

        void ReduceStaminaFromStat(uint creature, StatType trigger, int amount);

        int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested);

        int RestoreAbilityHitStaminaFromStat(uint creature, StatType trigger, int requested, AbilityDetail ability);

        int RefundCriticalRangedAbilityStaminaCost(uint creature, AbilityDetail ability);

        void ApplyAbilityStaminaCostFPRestore(uint creature, AbilityDetail ability, int staminaCost);

        bool TryGetAbilityStaminaCostState(
            uint creature,
            AbilityDetail ability,
            out CombatAbilityStaminaCostState state);

        /// <summary>The cost paid by this activation, before any hit refunds.</summary>
        int GetAbilityStaminaPaid(uint creature, AbilityDetail ability);

        /// <summary>Hit rewards share the actual activation's stamina spend, retaining at least
        /// one stamina of cost. FP-funded abilities and explicit recovery actions keep their payouts.</summary>
        int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool sendFeedback = true,
            bool retainMinimumCost = true);

        int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability);

        void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability);

        /// <summary>
        /// Captures this activation's stamina cost context for impacts that land after the cast ends, such as
        /// scheduled device pulses. Returns a wrapper that runs an impact with that activation's context in place,
        /// so its hit rewards share the stamina the activation paid and a later activation of the same ability
        /// cannot be charged for them. Returns null when the activation paid no stamina.
        /// </summary>
        Action<Action> CaptureAbilityStaminaCostContext(uint creature, AbilityDetail ability);

        void CompleteAbilityStaminaCostContext(uint creature, AbilityDetail ability);

        void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability);

        void ApplyAbilityFPCostStaminaRestore(uint creature, AbilityDetail ability, int fpCost);

        int GetNextAbilityFPCostAdjustment(uint creature, SkillType skillType);

        int ConsumeNextAbilityFPCostAdjustment(uint creature, SkillType skillType);

        void ApplyAbilityRestoredFPEffects(uint activator);

        void ApplyAbilityRestoredBothResourcesEffects(uint activator);

        bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent);

        bool IsCurrentFPAndStaminaAbovePercent(
            uint creature,
            int thresholdPercent,
            AbilityDetail ability = null);

        bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent);

        void ForgetResourceTriggerState(uint creature);

        void ForgetAbilityStaminaCostState(uint creature);
    }
}

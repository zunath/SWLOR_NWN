using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Attack timing for combat: swing delays, the attacks each swing resolves and the debt carried between swings, and
    /// the one-shot no-delay, critical-rate and next-ability buffs.
    /// </summary>
    public interface ICombatAttackTimingService
    {
        /// <summary>
        /// Consumes the armed next-ability delay buff and combines it with active limited Haste,
        /// returning the percent (1-100) the activation delay is reduced by, or 0 when neither
        /// applies. An armed buff with no partial-reduction partner removes the delay entirely
        /// (100). A currently active ranged no-delay status preserves the temporary arm because
        /// consuming it would provide no additional timing benefit.
        /// </summary>
        int ConsumeNextAbilityDelayReductionPercent(uint creature, AbilityDetail ability);

        bool HasNextAutoAttackNoDelay(uint creature, SkillType skillType);

        bool HasTemporaryNextAutoAttackNoDelay(uint creature, SkillType skillType);

        bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType);

        int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType);

        bool IsAttackDelayReductionSuppressed(uint creature);

        void GrantNextAutoAttackNoDelay(uint creature, SkillType skillType, int durationSeconds);

        void GrantNextAutoAttackNoDelay(uint creature, int durationSeconds);

        void GrantNextAutoAttackCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment,
            int durationSeconds);

        void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds);

        /// <summary>
        /// Arms the next matching ability's delay buff. A <paramref name="delayReductionPercent"/>
        /// of 1-99 reduces the activation delay by that percent; 0 or 100+ removes it entirely.
        /// </summary>
        void GrantNextAbilityNoDelay(
            uint creature,
            SkillType skillType,
            int durationSeconds,
            int delayReductionPercent = 0);

        void GrantNextAbilityDamageBonus(uint creature, PerkType perkType, int bonus, int durationSeconds);

        void GrantNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds,
            int defenseIgnorePercentAdjustment = 0,
            StatusEffectCategory requiredCategory = StatusEffectCategory.None);

        void GrantStatusAppliedNextAttackDamageBonus(
            uint creature,
            int damageBonus,
            int durationSeconds);

        void GrantCuedNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false);

        void ShowReadyCue(uint creature, bool wasArmed, int bonus, string text);

        void GrantNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds,
            string group = null,
            bool rangedOnly = false,
            bool hostileOnly = false);

        void ApplyBleedingStatusExpiredEffects(uint source);

        void GrantNextSkillAutoAttackDamageBonus(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int durationSeconds);

        void GrantNextAbilityFPCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds);

        /// <summary>
        /// Calculates the attack delay for a creature based on equipped weapon delay item properties.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay for.</param>
        /// <returns>Attack delay in milliseconds.</returns>
        int CalculateAttackDelay(uint attacker);

        /// <summary>
        /// Calculates attack delay while adjusting the creature's raw attack-delay reduction.
        /// A negative adjustment is used to compare a swing with and without a limited-attack
        /// speed effect without mutating the creature's active status effects.
        /// </summary>
        int CalculateAttackDelay(uint attacker, int attackDelayReductionAdjustment);

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        int ConsumeAttacksPerSwing(uint attacker, int effectiveDelayMilliseconds);

        /// <summary>
        /// Determines how many attacks the attacker's next swing should resolve and updates the
        /// attacker's carried fractional attack debt.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        /// <param name="effectiveDelayMilliseconds">The effective per-attack delay in milliseconds.</param>
        /// <param name="unbuffedDelayMilliseconds">
        /// The effective delay the attacker would have without a no-delay buff, used to size the
        /// guarantee below.
        /// </param>
        /// <param name="hasNoDelayBuff">
        /// Whether a no-delay buff was consumed for this swing. When set, the buff must be worth at
        /// least one extra attack. Without that guarantee a no-delay buff only lowers the delay to
        /// <see cref="MinimumAttackDelayMilliseconds"/>, which does nothing at all for a build
        /// already sitting at that floor (heavily hasted or dual-wielding). This must be passed
        /// explicitly rather than inferred from the two delays differing: a build already at the
        /// floor supplies equal values, which is exactly the case the guarantee exists to fix.
        /// </param>
        /// <returns>The number of attacks to resolve in this swing.</returns>
        int ConsumeAttacksPerSwing(
            uint attacker,
            int effectiveDelayMilliseconds,
            int unbuffedDelayMilliseconds,
            bool hasNoDelayBuff);

        /// <summary>
        /// Determines the attacks resolved by a swing while preventing a limited-attack speed
        /// effect from pre-scheduling more accelerated attacks than its remaining charges cover.
        /// </summary>
        int ConsumeAttacksPerSwing(
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
            LimitedAttackTimingBudget temporaryNoDelayBudget = default);

        /// <summary>
        /// Clears any carried fractional attack debt for a creature. Used when combat ends,
        /// the creature becomes unable to attack, or its equipped weapons change.
        /// </summary>
        /// <param name="attacker">The attacking creature.</param>
        void ClearAttackSwingDebt(uint attacker);

        /// <summary>
        /// Handles paralyze status effects for a creature before resolving a delayed attack.
        /// </summary>
        /// <param name="attacker">The creature to check for paralyze.</param>
        /// <returns>True if the creature is paralyzed and cannot act.</returns>
        bool HandleParalyze(uint attacker);

        /// <summary>
        /// Calculates the attack delay reduction percentage based on stat adjustments and active speed effects.
        /// Cumulative reductions are capped at 50%.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay reduction for.</param>
        /// <returns>Attack delay reduction percentage.</returns>
        int CalculateAttackDelayReduction(uint attacker);

        /// <summary>
        /// Attack delay reduction that depends on the swing's target, such as Flurry Stance's Haste
        /// for Throwing auto-attacks against bleeding targets. Positive values respect Haste suppression.
        /// </summary>
        int GetTargetAttackDelayReduction(uint attacker, uint target, SkillType skillType);

        int CalculateOffhandAttackDelayReduction(uint attacker);

        /// <summary>The weapon's Delay item property total (the value shown on examine), or 0 for no weapon.</summary>
        int GetWeaponDelay(uint item);

        void ForgetAttackTimingState(uint creature);
    }
}

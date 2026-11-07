using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    // The formulas live in Domain/Combat/CombatRules.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
        /// <inheritdoc cref="CombatRules.CalculateDamageRange"/>
        public static (int, int) CalculateDamageRange(int attackerAttack, int attackerDMG, int attackerStat, int defenderDefense, int defenderStat, int critical, int deltaCap = 0) =>
            CombatRules.CalculateDamageRange(attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);

        /// <inheritdoc cref="CombatRules.CalculateHitRate"/>
        public static int CalculateHitRate(int attackerAccuracy, int defenderEvasion, int percentageModifier, int minimumHitRate = MinimumHitRate) =>
            CombatRules.CalculateHitRate(attackerAccuracy, defenderEvasion, percentageModifier, minimumHitRate);

        /// <inheritdoc cref="CombatRules.CalculateCriticalRate"/>
        public static int CalculateCriticalRate(int attackerPER, int defenderVIT, int skillRank, int criticalModifier) =>
            CombatRules.CalculateCriticalRate(attackerPER, defenderVIT, skillRank, criticalModifier);

        public static int ClampCriticalDamagePercentAdjustment(int adjustment) => CombatRules.ClampCriticalDamagePercentAdjustment(adjustment);

        private static StatType GetDamageReflectionStat(CombatDamageType damageType) => CombatRules.GetDamageReflectionStat(damageType);

        public static int ApplyOrdinaryAttackDamageMultiplier(int damage, int percentAdjustment) =>
            CombatRules.ApplyOrdinaryAttackDamageMultiplier(damage, percentAdjustment);

        /// <inheritdoc cref="CombatRules.CapOutgoingDamageBonus"/>
        public static int CapOutgoingDamageBonus(int baseDamage, int adjustedDamage, int flatBonus, int maximumBonusPercent = MaximumDamageBonusPercent) =>
            CombatRules.CapOutgoingDamageBonus(baseDamage, adjustedDamage, flatBonus, maximumBonusPercent);

        /// <inheritdoc cref="CombatRules.ApplyCombinedDamageTakenAdjustment"/>
        public static int ApplyCombinedDamageTakenAdjustment(int damage, int targetStatusAdjustment, int genericAdjustment) =>
            CombatRules.ApplyCombinedDamageTakenAdjustment(damage, targetStatusAdjustment, genericAdjustment);

        public static int CalculateAutoAttackProcDamage(IEnumerable<StatAdjustmentSource> sources, Func<int> roll) =>
            CombatRules.CalculateAutoAttackProcDamage(sources, roll);

        public static bool CanTriggerAutoAttackSplash(StatAdjustmentSource source, SkillType skillType) =>
            CombatRules.CanTriggerAutoAttackSplash(source, skillType);

        public static uint[] SelectAutoAttackSplashSecondaryTargets(IEnumerable<uint> candidates, uint primaryTarget, int maximumTotalTargets) =>
            CombatRules.SelectAutoAttackSplashSecondaryTargets(candidates, primaryTarget, maximumTotalTargets);

        /// <inheritdoc cref="CombatRules.SelectSecondaryDamageTargets"/>
        public static uint[] SelectSecondaryDamageTargets(IEnumerable<uint> candidates, uint primaryTarget, int maximumSecondaryTargets) =>
            CombatRules.SelectSecondaryDamageTargets(candidates, primaryTarget, maximumSecondaryTargets);

        public static int CalculateMaxHPHealingBudget(int maximumHP, int percent) => CombatRules.CalculateMaxHPHealingBudget(maximumHP, percent);

        public static int CalculateDamageEligibleForHealing(int damage, int targetHP, int pendingDamage = 0) =>
            CombatRules.CalculateDamageEligibleForHealing(damage, targetHP, pendingDamage);

        public static int CalculateCappedDamageDerivedHealingAmount(int damage, int healingAlreadyApplied, int requestedHealing) =>
            CombatRules.CalculateCappedDamageDerivedHealingAmount(damage, healingAlreadyApplied, requestedHealing);

        public static int ApplyTargetHPDamageAdjustment(int damage, int currentHP, int maxHP, int thresholdPercent, int adjustmentPercent) =>
            CombatRules.ApplyTargetHPDamageAdjustment(damage, currentHP, maxHP, thresholdPercent, adjustmentPercent);

        public static int ApplyPercentDamageAdjustment(int damage, int adjustment) => CombatRules.ApplyPercentDamageAdjustment(damage, adjustment);

        /// <inheritdoc cref="CombatRules.GetIncomingPhysicalToForceConversionPortion"/>
        public static int GetIncomingPhysicalToForceConversionPortion(int physicalDamage, int conversionPercent) =>
            CombatRules.GetIncomingPhysicalToForceConversionPortion(physicalDamage, conversionPercent);

        public static bool IsRangedDamageSkill(SkillType skillType) => CombatRules.IsRangedDamageSkill(skillType);

        public static bool IsRangedWeaponSkill(SkillType skillType) => CombatRules.IsRangedWeaponSkill(skillType);

        public static float GetWeaponEngagementRange(SkillType skillType) => CombatRules.GetWeaponEngagementRange(skillType);

        public static bool IsMeleeWeaponSkill(SkillType skillType) => CombatRules.IsMeleeWeaponSkill(skillType);

        /// <inheritdoc cref="CombatRules.CanTriggerBleedingTargetAbilitySplash"/>
        public static bool CanTriggerBleedingTargetAbilitySplash(StatAdjustmentSource source, SkillType skillType, bool isBleeding, int roll) =>
            CombatRules.CanTriggerBleedingTargetAbilitySplash(source, skillType, isBleeding, roll);

        private static Type GetSourceStatusStackEffectType(StatusEffectCategory category) => CombatRules.GetSourceStatusStackEffectType(category);

        private static bool IsTransferableHarmfulStatus(IStatusEffect effect) => CombatRules.IsTransferableHarmfulStatus(effect);

        internal static bool IsSuccessfulHostileAbilityImpact(AbilityDetail ability, int damage, bool statusApplied) =>
            CombatRules.IsSuccessfulHostileAbilityImpact(ability, damage, statusApplied);

        /// <inheritdoc cref="CombatRules.ApplyTriggeredDamageTargetAdjustment"/>
        public static (int Damage, int Adjustment) ApplyTriggeredDamageTargetAdjustment(int damage, int typedTargetAdjustment, int? previouslyAppliedAdjustment) =>
            CombatRules.ApplyTriggeredDamageTargetAdjustment(damage, typedTargetAdjustment, previouslyAppliedAdjustment);

        private const int DeflectingReturnCooldownSeconds = CombatRules.DeflectingReturnCooldownSeconds;

        /// <inheritdoc cref="CombatRules.GetRangedDeflectionReflectionAmount"/>
        public static int GetRangedDeflectionReflectionAmount(int attackWeaponDamage, int reflectPercent, int deflectorWeaponDamage, int capPercent) =>
            CombatRules.GetRangedDeflectionReflectionAmount(attackWeaponDamage, reflectPercent, deflectorWeaponDamage, capPercent);

        public static (int ReflectPercent, int CapPercent) GetRangedDeflectionReflectionRates(int baseReflectPercent, int baseCapPercent, int embattledStacks, int embattledHighStackThreshold, int embattledHighStackBonusPercent, int overrideReflectPercent, int overrideCapPercent) =>
            CombatRules.GetRangedDeflectionReflectionRates(baseReflectPercent, baseCapPercent, embattledStacks, embattledHighStackThreshold, embattledHighStackBonusPercent, overrideReflectPercent, overrideCapPercent);

        /// <inheritdoc cref="CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints"/>
        public static int CalculateHitPointsSpentThroughTemporaryHitPoints(int payment, int temporaryHitPoints) =>
            CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(payment, temporaryHitPoints);

        /// <inheritdoc cref="CombatRules.CalculateHitPointSpendStaminaRestore"/>
        public static int CalculateHitPointSpendStaminaRestore(int maxStamina, int percent, int hitPointsSpent, int hitPointCost) =>
            CombatRules.CalculateHitPointSpendStaminaRestore(maxStamina, percent, hitPointsSpent, hitPointCost);

        public static int CalculateAbilityCriticalChance(int totalPercentAdjustment) =>
            CombatRules.CalculateAbilityCriticalChance(totalPercentAdjustment);

        /// <inheritdoc cref="CombatRules.SumSkillSelectedStatAdjustment"/>
        public static int SumSkillSelectedStatAdjustment(IReadOnlyList<StatAdjustmentSource> sources, SkillType skillType, StatType skillTypeStat, StatType adjustmentStat, bool unselectedSourcesApplyToEverySkill = false) =>
            CombatRules.SumSkillSelectedStatAdjustment(sources, skillType, skillTypeStat, adjustmentStat, unselectedSourcesApplyToEverySkill);

        public static int ApplyDefenseIgnore(int defense, int defenseIgnorePercent) => CombatRules.ApplyDefenseIgnore(defense, defenseIgnorePercent);

        public static bool CanConsumeNextAbilityNoDelay(AbilityDetail ability) => CombatRules.CanConsumeNextAbilityNoDelay(ability);

        public static bool MatchesStaminaDiscount(StatAdjustmentSource source, SkillType skillType, bool isHostileAbility) =>
            CombatRules.MatchesStaminaDiscount(source, skillType, isHostileAbility);

        public static bool MatchesNextSkillAbilityCriticalRateSkill(SkillType skillType, SkillType requiredSkillType, bool rangedOnly) =>
            CombatRules.MatchesNextSkillAbilityCriticalRateSkill(skillType, requiredSkillType, rangedOnly);

        public static int CalculateAbilityHitStaminaRestore(int cost, int refunded, int requested) =>
            CombatRules.CalculateAbilityHitStaminaRestore(cost, refunded, requested);

        private static int CalculateResourceRestoreFromCost(int cost, int percent) => CombatRules.CalculateResourceRestoreFromCost(cost, percent);

        private static RecastGroup GetRecastGroupFromStat(int value) => CombatRules.GetRecastGroupFromStat(value);

        private static SkillType GetSkillTypeFromStat(int value) => CombatRules.GetSkillTypeFromStat(value);

        private static StatusEffectCategory GetStatusEffectCategoryFromStat(int value) => CombatRules.GetStatusEffectCategoryFromStat(value);

        private static EffectIconType GetEffectIconTypeFromStat(int value) => CombatRules.GetEffectIconTypeFromStat(value);

        private static StatusEffectCleanseType GetStatusEffectCleanseTypeFromStat(int value) => CombatRules.GetStatusEffectCleanseTypeFromStat(value);

        private static ResistanceType GetResistanceTypeFromStat(int value) => CombatRules.GetResistanceTypeFromStat(value);

        private static CombatDamageType GetCombatDamageTypeFromStat(int value) => CombatRules.GetCombatDamageTypeFromStat(value);

        private static AbilityType GetAbilityTypeFromStatPlusOne(int value) => CombatRules.GetAbilityTypeFromStatPlusOne(value);

        private static bool SkillTypeMatches(SkillType actualSkillType, SkillType requiredSkillType) =>
            CombatRules.SkillTypeMatches(actualSkillType, requiredSkillType);

        private static bool SkillTypeMatchesOrGlobal(SkillType actualSkillType, SkillType requiredSkillType) =>
            CombatRules.SkillTypeMatchesOrGlobal(actualSkillType, requiredSkillType);

        /// <inheritdoc cref="CombatRules.AbilityUsedTriggerSkillMatches"/>
        public static bool AbilityUsedTriggerSkillMatches(SkillType abilitySkillType, SkillType requiredSkillType, bool rangedOnly, bool isHostileAbility) =>
            CombatRules.AbilityUsedTriggerSkillMatches(abilitySkillType, requiredSkillType, rangedOnly, isHostileAbility);

        public static bool CanWeaponSkillTriggerAbility(SkillType weaponSkillType, SkillType abilitySkillType) =>
            CombatRules.CanWeaponSkillTriggerAbility(weaponSkillType, abilitySkillType);

        public static bool IsWeaponSkillType(SkillType skillType) => CombatRules.IsWeaponSkillType(skillType);

        private static PerkType GetPerkTypeFromStat(int value) => CombatRules.GetPerkTypeFromStat(value);

        private static int GetTargetedStatGroupAdjustment(StatGroup statGroup, PerkType perkType, StatType primaryPerkStatType, StatType secondaryPerkStatType, StatType adjustmentStatType) =>
            CombatRules.GetTargetedStatGroupAdjustment(statGroup, perkType, primaryPerkStatType, secondaryPerkStatType, adjustmentStatType);

        private static string GetPerkTypeGroup(PerkType perkType) => CombatRules.GetPerkTypeGroup(perkType);

        internal static string GetDeflectionResultName(DeflectionSource source) => CombatRules.GetDeflectionResultName(source);

        /// <inheritdoc cref="CombatRules.CalculateAttackDelayMilliseconds"/>
        public static int CalculateAttackDelayMilliseconds(int rightHandDelayUnits, int leftHandDelayUnits, int attackDelayReductionPercent, int offhandAttackDelayReductionPercent) =>
            CombatRules.CalculateAttackDelayMilliseconds(rightHandDelayUnits, leftHandDelayUnits, attackDelayReductionPercent, offhandAttackDelayReductionPercent);

        /// <inheritdoc cref="CombatRules.CalculateEffectiveAttackDelay(int)"/>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds) =>
            CombatRules.CalculateEffectiveAttackDelay(attackerDelayMilliseconds);

        /// <inheritdoc cref="CombatRules.CalculateEffectiveAttackDelay(int, bool)"/>
        public static int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds, bool useDefaultMinimumDelay) =>
            CombatRules.CalculateEffectiveAttackDelay(attackerDelayMilliseconds, useDefaultMinimumDelay);

        /// <inheritdoc cref="CombatRules.CalculateAttackSwingDelay"/>
        public static int CalculateAttackSwingDelay(int effectiveDelayMilliseconds) =>
            CombatRules.CalculateAttackSwingDelay(effectiveDelayMilliseconds);

        /// <inheritdoc cref="CombatRules.CalculateAttacksPerSwing"/>
        public static int CalculateAttacksPerSwing(int effectiveDelayMilliseconds, float attackDebt, out float updatedAttackDebt) =>
            CombatRules.CalculateAttacksPerSwing(effectiveDelayMilliseconds, attackDebt, out updatedAttackDebt);

        public static int CapAttacksPerSwingForLimitedAttackEffect(int acceleratedAttacks, int baselineAttacks, int remainingAttacks, int limitedNoDelayRemainingAttacks = 0) =>
            CombatRules.CapAttacksPerSwingForLimitedAttackEffect(acceleratedAttacks, baselineAttacks, remainingAttacks, limitedNoDelayRemainingAttacks);

        // Stays on Combat: it draws from the shared Random generator, so it is not a pure rule.
        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's raw defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static facade every caller of combat uses. Each member forwards to the part of <c>Domain/Combat</c> that owns it
    /// (<c>Combat*Service</c>, listed as parts of <c>Combat</c> in <c>ConvertedServices.json</c>), to <see cref="ICombatService"/>
    /// for what is left, or to the pure <see cref="CombatRules"/>, so no caller changes and no call goes through another
    /// service first. The handlers stay here under their names, and the constants repeat the ones their owners declare.
    /// </summary>
    public static class Combat
    {
        public const int BaseGuardDamageReductionPercent = CombatDamageTakenService.BaseGuardDamageReductionPercent;
        public const int MaximumGuardDamageReductionPercent = CombatDamageTakenService.MaximumGuardDamageReductionPercent;
        public const int MaximumNormalDamageReductionPercent = CombatRules.MaximumNormalDamageReductionPercent;
        public const int MaximumDamageBonusPercent = CombatRules.MaximumDamageBonusPercent;
        public const int MaximumCombinedDamageReductionPercent = CombatRules.MaximumCombinedDamageReductionPercent;
        public const int MaximumAttackDelayAdjustmentPercent = CombatRules.MaximumAttackDelayAdjustmentPercent;
        public const int BaseHitRate = CombatRules.BaseHitRate;
        public const int MinimumHitRate = CombatRules.MinimumHitRate;
        /// <summary>A boss's floor, so stacking Evasion can't make a boss harmless.</summary>
        public const int BossMinimumHitRate = CombatQueriesService.BossMinimumHitRate;
        public const int MaximumHitRate = CombatRules.MaximumHitRate;
        public const int MinimumCriticalRate = CombatRules.MinimumCriticalRate;
        public const int MaximumCriticalRate = CombatRules.MaximumCriticalRate;
        public const int MaximumDamageDerivedHealingPercentPerHit = CombatRules.MaximumDamageDerivedHealingPercentPerHit;
        public const int MaximumActivatedDamageHealingMaxHPPercent = CombatResourcesService.MaximumActivatedDamageHealingMaxHPPercent;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerHit = CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerHit;
        public const int MaximumCooldownDamageHealingMaxHPPercentPerHit = CombatResourcesService.MaximumCooldownDamageHealingMaxHPPercentPerHit;
        public const int MaximumPassiveDamageHealingMaxHPPercentPerWindow = CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerWindow;
        public const int MaximumDefeatedEnemyHealingMaxHPPercentPerWindow = CombatResourcesService.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow;
        public const int CombatHealingWindowSeconds = CombatResourcesService.CombatHealingWindowSeconds;
        public const int MaximumCriticalDamagePercentAdjustment = CombatRules.MaximumCriticalDamagePercentAdjustment;
        public const int StandardCriticalRating = CombatRules.StandardCriticalRating;
        public const int BaseAttackDelayMilliseconds = CombatRules.BaseAttackDelayMilliseconds;
        public const float MeleeWeaponEngagementRange = CombatRules.MeleeWeaponEngagementRange;
        public const float RangedWeaponEngagementRange = CombatRules.RangedWeaponEngagementRange;
        public const int MaxAttacksPerSwing = CombatRules.MaxAttacksPerSwing;
        public const int MinimumAttackDelayMilliseconds = CombatRules.MinimumAttackDelayMilliseconds;

        // The formulas live in Domain/Combat/CombatRules.cs. These forwarders keep every member on Combat for its callers.
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

        // The damage type catalog lives in Domain/Combat/CombatDamageCatalogService.cs. These forwarders keep every member on Combat
        // for its callers, and the two handlers stay here under their names.
        private static ICombatDamageCatalogService DamageCatalog => FacadeAccessor.Resolve<ICombatDamageCatalogService>();

        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadDamageTypes()
        {
            DamageCatalog.LoadDamageTypes();
        }

        /// <summary>
        /// When a player enters the server, apply any defense and resistance entries they don't already have.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddDamageTypeResistances()
        {
            DamageCatalog.AddDamageTypeResistances();
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetAllDamageTypes()
        {
            return DamageCatalog.GetAllDamageTypes();
        }

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetDefenseDamageTypes()
        {
            return DamageCatalog.GetDefenseDamageTypes();
        }

        // Pure and needs no container: the Beast, IncubationJob and BeastLevel constructors call this one.
        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0) =>
            CombatRules.CreateDefaultDefenseValues(defaultValue);

        public static bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0)
        {
            return DamageCatalog.EnsureDefenseValues(defenses, defaultValue);
        }

        // The combat log lines live in Domain/Combat/CombatFeedbackService.cs. These forwarders keep every member on Combat for its callers.
        private static ICombatFeedbackService Feedback => FacadeAccessor.Resolve<ICombatFeedbackService>();
        private static void SendGuardedHitFeedback(uint defender, uint attacker, int preventedDamage) =>
            Feedback.SendGuardedHitFeedback(defender, attacker, preventedDamage);

        public static void SendIncomingCriticalHitDowngradeFeedback(uint attacker, uint defender) =>
            Feedback.SendIncomingCriticalHitDowngradeFeedback(attacker, defender);

        /// <inheritdoc cref="ICombatFeedbackService.BuildCombatLogMessage"/>
        public static string BuildCombatLogMessage(uint observer, uint attacker, uint defender, int attackResultType, int chanceToHit) =>
            Feedback.BuildCombatLogMessage(observer, attacker, defender, attackResultType, chanceToHit);

        public static string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
            Feedback.BuildAbilityCombatLogMessage(observer, attacker, defender, abilityName, attackResultType, chanceToHit);

        public static string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) =>
            Feedback.BuildAbilityNoTargetCombatLogMessage(observer, attacker, abilityName);

        public static void SendAbilityCriticalHitFeedback(uint attacker, uint defender, string abilityName) =>
            Feedback.SendAbilityCriticalHitFeedback(attacker, defender, abilityName);

        public static string BuildAbilityCriticalHitCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName) =>
            Feedback.BuildAbilityCriticalHitCombatLogMessage(observer, attacker, defender, abilityName);

        public static void SendTemporaryHitPointDamageFeedback(uint attacker, uint defender, int damage) =>
            Feedback.SendTemporaryHitPointDamageFeedback(attacker, defender, damage);

        /// <inheritdoc cref="ICombatFeedbackService.BuildCombatLogMessageNative"/>
        public static string BuildCombatLogMessageNative(uint observer, CNWSCreature attacker, CNWSCreature defender, int attackResultType, int chanceToHit, DeflectionSource deflectionSource = DeflectionSource.None) =>
            Feedback.BuildCombatLogMessageNative(observer, attacker, defender, attackResultType, chanceToHit, deflectionSource);

        // The combat queries live in Domain/Combat/CombatQueriesService.cs. These forwarders keep every member on Combat for its callers.
        private static ICombatQueriesService Queries => FacadeAccessor.Resolve<ICombatQueriesService>();

        internal static WeaponImpactProfile GetWeaponImpactProfile(uint actor) => Queries.GetWeaponImpactProfile(actor);

        internal static void SetWeaponImpactProfile(uint actor, WeaponImpactProfile profile) => Queries.SetWeaponImpactProfile(actor, profile);

        internal static void RemoveWeaponImpactProfile(uint actor) => Queries.RemoveWeaponImpactProfile(actor);
        /// <inheritdoc cref="ICombatQueriesService.GetMinimumHitRate"/>
        public static int GetMinimumHitRate(uint attacker) => Queries.GetMinimumHitRate(attacker);

        public static int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType) =>
            Queries.GetRangedAttackDamageFlatAdjustment(attacker, skillType);

        public static int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType) =>
            Queries.GetRangedAttackDefenseIgnorePercentAdjustment(attacker, skillType);

        private static bool IsMatchingBackAttack(uint attacker, uint defender, SkillType skillType) =>
            Queries.IsMatchingBackAttack(attacker, defender, skillType);

        private static bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType) =>
            Queries.IsMatchingSideAttack(attacker, defender, skillType);

        public static bool IsAttackerBesideTarget(uint attacker, uint defender) => Queries.IsAttackerBesideTarget(attacker, defender);

        public static bool IsAttackerBehindTarget(uint attacker, uint defender) => Queries.IsAttackerBehindTarget(attacker, defender);

        private static uint GetNearestHostileCreatureWithinRange(uint source, uint origin, float radius, uint excludedTarget = OBJECT_INVALID) =>
            Queries.GetNearestHostileCreatureWithinRange(source, origin, radius, excludedTarget);

        private static bool IsStealthedOrInvisible(uint creature) => Queries.IsStealthedOrInvisible(creature);

        public static bool HasDamageImmunity(uint defender, CombatDamageType damageType) => Queries.HasDamageImmunity(defender, damageType);

        private static bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance) =>
            Queries.IsNearbyTargetWithinDistance(attacker, defender, distance);

        private static int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType) =>
            Queries.GetStatusSourceStatAdjustment(creature, source, statType);

        private static int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType) =>
            Queries.GetStatusSourcePartyStatAdjustment(creature, attacker, statType);

        private static int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender) =>
            Queries.GetDamageToSourceAppliedStatusTargetAdjustment(attacker, defender);

        private static int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender, SkillType skillType) =>
            Queries.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(attacker, defender, skillType);

        private static int GetAbilityDamageToSourceAppliedStatusTargetBonus(uint attacker, uint defender) =>
            Queries.GetAbilityDamageToSourceAppliedStatusTargetBonus(attacker, defender);

        private static bool TargetHasSourceAppliedStatusCategory(uint defender, uint source, StatusEffectCategory category) =>
            Queries.TargetHasSourceAppliedStatusCategory(defender, source, category);

        private static bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category) =>
            Queries.TargetHasAnyStatusEffectCategory(creature, category);

        private static bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches) =>
            Queries.AbilityAppliedAnyStatus(primaryStatusEffect, additionalStatusEffects, matches);

        private static bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category) =>
            Queries.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, category);

        public static uint GetAbilityAttackWeapon(uint creature, SkillType skillType) => Queries.GetAbilityAttackWeapon(creature, skillType);

        private static uint GetRelevantSkillWeapon(uint creature, SkillType skillType) => Queries.GetRelevantSkillWeapon(creature, skillType);

        /// <inheritdoc cref="ICombatQueriesService.GetSkillSelectedStatAdjustment"/>
        private static int GetSkillSelectedStatAdjustment(uint creature, SkillType skillType, StatType skillTypeStat, StatType adjustmentStat, bool unselectedSourcesApplyToEverySkill = false) =>
            Queries.GetSkillSelectedStatAdjustment(creature, skillType, skillTypeStat, adjustmentStat, unselectedSourcesApplyToEverySkill);

        private static bool IsRangedAbilityLongRangeTarget(uint creature, uint defender, SkillType skillType) =>
            Queries.IsRangedAbilityLongRangeTarget(creature, defender, skillType);

        private static int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType) =>
            Queries.GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);

        public static bool IsTargetNotFacingAttacker(uint attacker, uint defender) => Queries.IsTargetNotFacingAttacker(attacker, defender);

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability) => Queries.GetAbilitySkillType(creature, ability);

        public static SkillType GetEquippedWeaponSkillType(uint creature) => Queries.GetEquippedWeaponSkillType(creature);

        private static bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(uint creature, AbilityDetail ability) =>
            Queries.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(creature, ability);

        private static bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability) =>
            Queries.AbilityMatchesReversalCutTrigger(creature, ability);

        private static bool AbilityMatchesAnyPerkTypeStat(uint creature, AbilityDetail ability, params StatType[] statTypes) =>
            Queries.AbilityMatchesAnyPerkTypeStat(creature, ability, statTypes);

        private static bool AbilityMatchesPerkCategoryStat(uint creature, AbilityDetail ability, StatType categoryStatType) =>
            Queries.AbilityMatchesPerkCategoryStat(creature, ability, categoryStatType);

        private static bool AbilityUsedTriggerSkillMatches(uint activator, AbilityDetail ability, SkillType abilitySkillType, StatType skillTypeStat, StatType? rangedOnlyStat) =>
            Queries.AbilityUsedTriggerSkillMatches(activator, ability, abilitySkillType, skillTypeStat, rangedOnlyStat);

        private static bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType) =>
            Queries.IsWeaponOrForceDamage(skillType, damageType);

        public static int GetCombatImpactWeaponDamage(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false, bool requireMatchingSkill = false, int? triggeringWeaponDamage = null, bool abilityImpact = false) =>
            Queries.GetCombatImpactWeaponDamage(activator, skillType, usesQueuedNaturalWeapon, requireMatchingSkill, triggeringWeaponDamage, abilityImpact);

        /// <inheritdoc cref="ICombatQueriesService.IncludesCombatImpactWeaponDamage"/>
        public static bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon) =>
            Queries.IncludesCombatImpactWeaponDamage(skillType, usesQueuedNaturalWeapon);

        public static CombatDamageType GetCombatImpactWeaponDamageType(uint activator, SkillType skillType, bool usesQueuedNaturalWeapon = false) =>
            Queries.GetCombatImpactWeaponDamageType(activator, skillType, usesQueuedNaturalWeapon);

        public static bool HasEquippedWeaponForAbility(uint creature) => Queries.HasEquippedWeaponForAbility(creature);

        public static bool CanItemTriggerWeaponAbility(uint item, SkillType abilitySkillType) =>
            Queries.CanItemTriggerWeaponAbility(item, abilitySkillType);

        private static int GetTargetedAbilityAdjustment(uint creature, PerkType perkType, StatType primaryPerkStatType, StatType secondaryPerkStatType, StatType adjustmentStatType) =>
            Queries.GetTargetedAbilityAdjustment(creature, perkType, primaryPerkStatType, secondaryPerkStatType, adjustmentStatType);

        public static int GetPerkAdjustedAbilityScore(uint attacker) => Queries.GetPerkAdjustedAbilityScore(attacker);

        public static AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType) =>
            Queries.GetWeaponDamageAbilityType(creature, weaponType);

        public static AbilityType GetQueuedAbilityAccuracyAbilityType(uint creature, SkillType skillType) =>
            Queries.GetQueuedAbilityAccuracyAbilityType(creature, skillType);

        public static AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType) =>
            Queries.GetWeaponAccuracyAbilityType(creature, weaponType);

        public static int GetMiscDMGBonus(uint attacker, BaseItem weaponType) => Queries.GetMiscDMGBonus(attacker, weaponType);

        /// <inheritdoc cref="ICombatQueriesService.GetPowerAttackDMGBonus"/>
        public static int GetPowerAttackDMGBonus(uint attacker) => Queries.GetPowerAttackDMGBonus(attacker);

        // The resource bookkeeping lives in Domain/Combat/CombatResourcesService.cs. These forwarders keep every member on Combat for its callers.
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

        // The attack timing lives in Domain/Combat/CombatAttackTimingService.cs. These forwarders keep every member on Combat for its callers.
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

        // The Suppression stacks live in Domain/Combat/CombatSuppressionService.cs. These forwarders keep every member on Combat for its callers.
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

        // The combat activity records live in Domain/Combat/CombatActivityService.cs. These forwarders keep every member on Combat for its
        // callers, and the two handlers stay here under their names.
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

        // The ability and attack bonuses live in Domain/Combat/CombatAbilityBonusService.cs. These forwarders keep every member on Combat for
        // its callers.
        private static ICombatAbilityBonusService AbilityBonus => FacadeAccessor.Resolve<ICombatAbilityBonusService>();

        public static int PrepareOpeningAutoAttack(uint attacker, SkillType skillType) => AbilityBonus.PrepareOpeningAutoAttack(attacker, skillType);

        public static void PrepareQueuedWeaponAbilityOpeningAttackAtActivation(uint attacker, SkillType skillType) =>
            AbilityBonus.PrepareQueuedWeaponAbilityOpeningAttackAtActivation(attacker, skillType);

        public static int ConsumeOpeningAutoAttackCriticalDamageAdjustment(uint attacker) =>
            AbilityBonus.ConsumeOpeningAutoAttackCriticalDamageAdjustment(attacker);

        public static void ClearOpeningAutoAttackModifiers(uint attacker) => AbilityBonus.ClearOpeningAutoAttackModifiers(attacker);

        public static bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType) =>
            AbilityBonus.HasNextAbilityNoStaminaCost(creature, skillType);

        public static bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType) =>
            AbilityBonus.ConsumeNextAbilityNoStaminaCost(creature, skillType);

        public static int GetNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType, bool isHostileAbility = false) =>
            AbilityBonus.GetNextSkillAbilityStaminaCostAdjustment(creature, skillType, isHostileAbility);

        public static int ConsumeNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType, bool isHostileAbility = false) =>
            AbilityBonus.ConsumeNextSkillAbilityStaminaCostAdjustment(creature, skillType, isHostileAbility);

        /// <inheritdoc cref="ICombatAbilityBonusService.ConsumeNextSkillAbilityBonuses"/>
        public static (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            StatusEffectCategory abilityStatusCategories = StatusEffectCategory.None) =>
            AbilityBonus.ConsumeNextSkillAbilityBonuses(creature, skillType, abilityStatusCategories);

        public static int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) =>
            AbilityBonus.ConsumePersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        public static int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType) =>
            AbilityBonus.GetPersistentNextSkillAbilityCriticalRateBonus(creature, skillType);

        public static void StoreQueuedWeaponAbilityCriticalRateBonus(uint creature, SkillType skillType, int criticalRatePercentAdjustment) =>
            AbilityBonus.StoreQueuedWeaponAbilityCriticalRateBonus(creature, skillType, criticalRatePercentAdjustment);

        public static void StoreQueuedWeaponAbilityActivationCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment) =>
            AbilityBonus.StoreQueuedWeaponAbilityActivationCriticalRateBonus(creature, skillType, criticalRatePercentAdjustment);

        public static void StoreQueuedWeaponAbilityActivationIdleBonuses(uint creature, SkillType skillType) =>
            AbilityBonus.StoreQueuedWeaponAbilityActivationIdleBonuses(creature, skillType);

        public static void StoreWeaponAbilityActivationIdleBonuses(
            uint creature,
            bool isIdle,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int defenseIgnorePercent) =>
            AbilityBonus.StoreWeaponAbilityActivationIdleBonuses(creature, isIdle, damageBonus, criticalRatePercentAdjustment, defenseIgnorePercent);

        public static bool HasWeaponAbilityActivationIdleSnapshot(uint creature) => AbilityBonus.HasWeaponAbilityActivationIdleSnapshot(creature);

        public static int GetWeaponAbilityActivationIdleDamageBonus(uint creature) =>
            AbilityBonus.GetWeaponAbilityActivationIdleDamageBonus(creature);

        public static int GetWeaponAbilityActivationIdleCriticalRateBonus(uint creature) =>
            AbilityBonus.GetWeaponAbilityActivationIdleCriticalRateBonus(creature);

        public static int GetWeaponAbilityActivationIdleDefenseIgnorePercent(uint creature) =>
            AbilityBonus.GetWeaponAbilityActivationIdleDefenseIgnorePercent(creature);

        public static void ClearWeaponAbilityActivationIdleBonuses(uint creature) => AbilityBonus.ClearWeaponAbilityActivationIdleBonuses(creature);

        /// <inheritdoc cref="ICombatAbilityBonusService.GetQueuedWeaponAbilityActivationHitChanceAdjustment"/>
        public static int GetQueuedWeaponAbilityActivationHitChanceAdjustment(uint creature, SkillType skillType, AbilityDetail ability = null) =>
            AbilityBonus.GetQueuedWeaponAbilityActivationHitChanceAdjustment(creature, skillType, ability);

        public static void ClearQueuedWeaponAbilityActivationBonuses(uint creature) =>
            AbilityBonus.ClearQueuedWeaponAbilityActivationBonuses(creature);

        public static (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(
            uint creature,
            SkillType skillType) =>
            AbilityBonus.ConsumeQueuedWeaponAbilityBonuses(creature, skillType);

        public static void ClearQueuedWeaponAbilityAttemptBonuses(uint creature) => AbilityBonus.ClearQueuedWeaponAbilityAttemptBonuses(creature);

        public static (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(uint creature) =>
            AbilityBonus.ConsumeNextAttackGuardedHitBonuses(creature);

        public static (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature) =>
            AbilityBonus.ConsumeNextAttackGuardedHitAutoAttackBonuses(creature);

        public static int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature) =>
            AbilityBonus.ConsumeNextAttackGuardedHitCriticalRateBonus(creature);

        public static int ConsumeStatusAppliedNextAttackDamageBonus(uint creature) =>
            AbilityBonus.ConsumeStatusAppliedNextAttackDamageBonus(creature);

        public static int GetStatusAppliedNextAttackDamageBonus(uint creature) => AbilityBonus.GetStatusAppliedNextAttackDamageBonus(creature);

        public static void ApplyNextAttackGuardedHitEnmityBonus(uint attacker, uint defender, int enmityBonus) =>
            AbilityBonus.ApplyNextAttackGuardedHitEnmityBonus(attacker, defender, enmityBonus);

        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds) =>
            AbilityBonus.GrantNextAbilityDamageBonus(creature, perkTypeValue, bonus, durationSeconds);

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds) =>
            AbilityBonus.GrantNextSkillAbilityBonuses(creature, skillTypeValue, damageBonus, criticalRatePercentAdjustment, durationSeconds);

        public static int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType) =>
            AbilityBonus.ConsumeNextAbilityDamageBonus(creature, perkType);

        public static int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType) =>
            AbilityBonus.GetNextAbilityStaminaCostAdjustment(creature, perkType);

        public static int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType) =>
            AbilityBonus.ConsumeNextAbilityStaminaCostAdjustment(creature, perkType);

        public static int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType) =>
            AbilityBonus.GetAbilityDamageFlatAdjustment(creature, perkType, skillType);

        public static int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) =>
            AbilityBonus.GetAbilityStatusCategoryDamageBonus(creature, skillType, appliedCategories);

        public static int GetAbilityStatusCategoryHitChancePercentAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories) =>
            AbilityBonus.GetAbilityStatusCategoryHitChancePercentAdjustment(creature, skillType, appliedCategories);

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType) =>
            AbilityBonus.GetAbilityStaminaCostFlatAdjustment(creature, perkType);

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability) =>
            AbilityBonus.GetAbilityStaminaCostFlatAdjustment(creature, ability);

        public static int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory) =>
            AbilityBonus.GetAbilityStatusDurationPercentAdjustment(creature, perkType, skillType, primaryStatusEffect, additionalStatusEffects, statusEffectFactory);

        public static int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender) =>
            AbilityBonus.GetAbilityDefenseIgnorePercentAdjustment(creature, perkType, skillType, defender);

        public static float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType) =>
            AbilityBonus.GetAbilityRecastDelayFlatAdjustment(creature, perkType);

        public static float ApplyAbilityRecastDelayModifiers(uint activator, AbilityDetail ability, float recastDelaySeconds) =>
            AbilityBonus.ApplyAbilityRecastDelayModifiers(activator, ability, recastDelaySeconds);

        /// <inheritdoc cref="ICombatAbilityBonusService.GetAbilityDamageBonus"/>
        public static int GetAbilityDamageBonus(uint creature, SkillType skill) => AbilityBonus.GetAbilityDamageBonus(creature, skill);

        // The damage roll modifiers live in Domain/Combat/CombatDamageModifiersService.cs. These forwarders keep every member on Combat for its callers.
        private static ICombatDamageModifiersService DamageModifiers => FacadeAccessor.Resolve<ICombatDamageModifiersService>();

        public static (int Damage, int CriticalRating, bool WasCriticalDowngraded) CalculateDamageWithCriticalMitigation(
            uint defender,
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0) =>
            DamageModifiers.CalculateDamageWithCriticalMitigation(defender, attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);

        internal static bool TryUseIncomingCriticalHitDowngrade(uint defender, int criticalRating) =>
            DamageModifiers.TryUseIncomingCriticalHitDowngrade(defender, criticalRating);

        public static int ApplyCriticalDamageModifier(
            uint attacker,
            int damage,
            int criticalRating,
            SkillType skillType = SkillType.Invalid,
            uint defender = OBJECT_INVALID,
            int criticalDamagePercentAdjustment = 0) =>
            DamageModifiers.ApplyCriticalDamageModifier(attacker, damage, criticalRating, skillType, defender, criticalDamagePercentAdjustment);

        public static int GetSkillCriticalRatePercentAdjustment(uint attacker, SkillType skillType) =>
            DamageModifiers.GetSkillCriticalRatePercentAdjustment(attacker, skillType);

        public static int GetAutoAttackCriticalRateAdjustment(uint attacker, uint defender, int cycleCriticalRateAdjustment) =>
            DamageModifiers.GetAutoAttackCriticalRateAdjustment(attacker, defender, cycleCriticalRateAdjustment);

        public static int ApplyRangedAttackDefenseIgnore(uint attacker, int defense, SkillType skillType) =>
            DamageModifiers.ApplyRangedAttackDefenseIgnore(attacker, defense, skillType);

        public static int ApplyTargetStatusAttackModifiers(uint attacker, uint defender, int attack, SkillType skillType) =>
            DamageModifiers.ApplyTargetStatusAttackModifiers(attacker, defender, attack, skillType);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyDamageDealtModifiers(uint, uint, int, SkillType, CombatDamageType, bool, bool, bool, AbilityDetail, out int, int, int, bool)"/>
        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses,
            bool isLandedAttack,
            AbilityDetail ability,
            out int targetStatusDamagePercentAdjustment,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0,
            bool isOrdinaryWeaponAttack = false) =>
            DamageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability, out targetStatusDamagePercentAdjustment, abilityDamagePercentAdjustment, lowHPAbilityDamagePercentAdjustment, isOrdinaryWeaponAttack);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyOrdinaryAttackDamageMultiplier"/>
        public static int ApplyOrdinaryAttackDamageMultiplier(uint attacker, int damage) =>
            DamageModifiers.ApplyOrdinaryAttackDamageMultiplier(attacker, damage);

        public static int ApplyAbilityShapeDamageModifier(uint attacker, int damage, bool isAbilityDamage, AbilityDetail ability) =>
            DamageModifiers.ApplyAbilityShapeDamageModifier(attacker, damage, isAbilityDamage, ability);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyThrowingSingleTargetDamageModifier"/>
        public static int ApplyThrowingSingleTargetDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            AbilityDetail ability) =>
            DamageModifiers.ApplyThrowingSingleTargetDamageModifier(attacker, skillType, damage, isAbilityDamage, ability);

        /// <inheritdoc cref="ICombatDamageModifiersService.ApplyMimicryAbilityDamageModifier"/>
        public static int ApplyMimicryAbilityDamageModifier(uint attacker, int damage, SkillType skillType, bool isAbilityDamage) =>
            DamageModifiers.ApplyMimicryAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);

        public static int ApplySkillAbilityDamageModifier(uint attacker, int damage, SkillType skillType, bool isAbilityDamage) =>
            DamageModifiers.ApplySkillAbilityDamageModifier(attacker, damage, skillType, isAbilityDamage);

        public static int ApplyDamageDealtModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            bool isAbilityDamage = false,
            bool canApplyRandomFlatBonuses = true,
            bool isLandedAttack = true,
            AbilityDetail ability = null) =>
            DamageModifiers.ApplyDamageDealtModifiers(attacker, defender, damage, skillType, damageType, isAbilityDamage, canApplyRandomFlatBonuses, isLandedAttack, ability);

        public static int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplySideAttackDamageModifier(attacker, defender, skillType, damage);

        public static int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetSideAttackHitChanceAdjustment(attacker, defender, skillType);

        public static int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);

        public static int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplyBackAttackDamageModifier(attacker, defender, skillType, damage);

        public static int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.GetBackAttackCriticalRateAdjustment(attacker, defender, skillType);

        public static int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion) =>
            DamageModifiers.ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

        public static int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType) =>
            DamageModifiers.PrepareAutoAttackCycleCriticalRate(attacker, skillType);

        private static void ClearAutoAttackCycleCriticalRateTracker(uint creature) =>
            DamageModifiers.ClearAutoAttackCycleCriticalRateTracker(creature);

        public static int GetLowHPAttackAdjustment(uint attacker) => DamageModifiers.GetLowHPAttackAdjustment(attacker);

        public static int GetLowFPAttackAdjustment(uint attacker) => DamageModifiers.GetLowFPAttackAdjustment(attacker);

        private static int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender) =>
            DamageModifiers.GetTargetStatusCriticalRateAdjustment(attacker, defender);

        public static int ApplyDamageTypeDealtModifiers(uint attacker, int damage, CombatDamageType damageType) =>
            DamageModifiers.ApplyDamageTypeDealtModifiers(attacker, damage, damageType);

        public static int GetNearbyStatusTargetAttackAdjustment(uint creature) => DamageModifiers.GetNearbyStatusTargetAttackAdjustment(creature);

        public static int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility) =>
            DamageModifiers.ApplyTwinBladeAbilityShapeDamageModifier(attacker, skillType, damage, isSingleTargetAbility, isAreaAbility);

        public static int ApplyThrowingAbilityShapeDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, isAreaAbility);

        public static int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility) =>
            DamageModifiers.ApplyPhysicalAbilityShapeDamageModifier(attacker, damageType, damage, isSingleTargetAbility);

        public static int ApplySkillAreaAbilityDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplySkillAreaAbilityDamageModifier(attacker, skillType, damage, isAreaAbility);

        public static int ApplyRangedAbilityLongRangeDamageModifier(uint attacker, uint defender, SkillType skillType, int damage) =>
            DamageModifiers.ApplyRangedAbilityLongRangeDamageModifier(attacker, defender, skillType, damage);

        public static int ApplyAreaAbilityAfterDeflectionDamageModifier(uint attacker, SkillType skillType, int damage, bool isAreaAbility) =>
            DamageModifiers.ApplyAreaAbilityAfterDeflectionDamageModifier(attacker, skillType, damage, isAreaAbility);

        private static void ClearRangedRepeatedTargetDamageTracker(uint creature) => DamageModifiers.ClearRangedRepeatedTargetDamageTracker(creature);

        private static void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType) =>
            DamageModifiers.ApplySameTargetPressureDamageEffects(attacker, defender, skillType);

        public static int GetSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType) =>
            DamageModifiers.GetSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType);

        public static void ConsumeSameTargetPressureWeaponAbilityDamageBonus(uint attacker, uint target, SkillType skillType, int damage) =>
            DamageModifiers.ConsumeSameTargetPressureWeaponAbilityDamageBonus(attacker, target, skillType, damage);

        public static int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense) =>
            DamageModifiers.ApplyStatusSourceDefenseModifiers(attacker, defender, defense);

        public static int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy) =>
            DamageModifiers.ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy);

        private static void ForgetDamageModifierState(uint creature) => DamageModifiers.ForgetDamageModifierState(creature);

        // The ability hit roll lives in Domain/Combat/CombatAbilityHitService.cs. These forwarders keep every member on Combat for its callers.
        private static ICombatAbilityHitService AbilityHit => FacadeAccessor.Resolve<ICombatAbilityHitService>();

        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID) =>
            AbilityHit.CalculateAbilityCriticalRating(attacker, skillType, isAreaAbility, criticalRateAdjustment, defender);

        private static void ApplyGuardedHitNextAttackEffects(uint creature) => AbilityHit.ApplyGuardedHitNextAttackEffects(creature);

        public static int GetAbilityCriticalRate(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID) =>
            AbilityHit.GetAbilityCriticalRate(attacker, skillType, isAreaAbility, criticalRateAdjustment, defender);

        /// <inheritdoc cref="ICombatAbilityHitService.SetAbilityHitResolutionOverride"/>
        public static void SetAbilityHitResolutionOverride(bool? forcedOutcome) => AbilityHit.SetAbilityHitResolutionOverride(forcedOutcome);

        /// <inheritdoc cref="ICombatAbilityHitService.SetAutoAttackHitResolutionOverride"/>
        public static void SetAutoAttackHitResolutionOverride(bool? forcedOutcome) => AbilityHit.SetAutoAttackHitResolutionOverride(forcedOutcome);

        /// <inheritdoc cref="ICombatAbilityHitService.GetAutoAttackHitResolutionOverride"/>
        public static bool? GetAutoAttackHitResolutionOverride() => AbilityHit.GetAutoAttackHitResolutionOverride();

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid,
            int accuracyFlatAdjustment = 0) =>
            AbilityHit.TryResolveAbilityHit(attacker, defender, skillType, perkType, out hitRate, hitChancePercentAdjustment, skillLevelOverride, statOverride, accuracyFlatAdjustment);

        public static void ApplyRangedAbilityTargetDefenseReduction(uint attacker, uint defender, SkillType skillType) =>
            AbilityHit.ApplyRangedAbilityTargetDefenseReduction(attacker, defender, skillType);

        public static int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender) =>
            AbilityHit.GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);

        public static int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender) =>
            AbilityHit.GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);

        // The hit effects live in Domain/Combat/CombatHitEffectsService.cs. These forwarders keep every member on Combat for its callers.
        private static ICombatHitEffectsService HitEffects => FacadeAccessor.Resolve<ICombatHitEffectsService>();

        public static int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid) =>
            HitEffects.ApplyAutoAttackDamageModifiers(attacker, defender, damage, skillType);

        public static void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType) =>
            HitEffects.ConsumeSuppressedAutoAttackDamageBonuses(attacker, skillType);

        /// <inheritdoc cref="ICombatHitEffectsService.ConsumeAutoAttackCycleDamageBonus"/>
        public static int ConsumeAutoAttackCycleDamageBonus(uint attacker, SkillType skillType) =>
            HitEffects.ConsumeAutoAttackCycleDamageBonus(attacker, skillType);

        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            bool isAbilityDamage = false,
            bool allowsHitProcs = true) =>
            HitEffects.ApplyDamageDealtEffects(attacker, defender, damage, skillType, damageType, deliveryType, isAbilityDamage, allowsHitProcs);

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid) =>
            HitEffects.ApplyCriticalHitEffects(attacker, defender, damage, criticalRating, isSingleTargetImpact, skillType);

        public static void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType) =>
            HitEffects.ApplyNonCriticalAbilityEffects(activator, target, skillType);

        public static void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID) =>
            HitEffects.ApplyDefeatedEnemyEffects(creature, defeated);

        private static void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated) => HitEffects.ApplyRecentDamagerDefeatedEnemyEffects(defeated);

        private static void ApplySourceStatusStackEffects(uint attacker, uint defender) =>
            HitEffects.ApplySourceStatusStackEffects(attacker, defender);

        private static void SpreadPoisonFromTarget(uint activator, uint target) => HitEffects.SpreadPoisonFromTarget(activator, target);

        public static int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid,
            bool typedLeadershipReductionAlreadyApplied = false,
            int? targetStatusDamagePercentAdjustment = null,
            bool outgoingModifiersAlreadyApplied = false) =>
            HitEffects.ApplyTriggeredDamage(activator, target, damage, damageType, skillType, typedLeadershipReductionAlreadyApplied, targetStatusDamagePercentAdjustment, outgoingModifiersAlreadyApplied);

        private static void ForgetHitEffectState(uint creature) => HitEffects.ForgetHitEffectState(creature);

        // The damage taken part lives in Domain/Combat/CombatDamageTakenService.cs. These forwarders keep every member on Combat for
        // its callers, and the damage handler stays here under its name.
        private static ICombatDamageTakenService DamageTaken => FacadeAccessor.Resolve<ICombatDamageTakenService>();

        public static int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false) =>
            DamageTaken.ApplyDamageTakenModifiers(defender, damage, attacker, damageType, deliveryType, targetStatusDamagePercentAdjustment, isLandedAttack, typedLeadershipReductionAlreadyApplied);

        public static int ApplyTypedLeadershipDamageTakenModifier(uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyTypedLeadershipDamageTakenModifier(defender, damage, damageType);

        public static void ApplyDamageReflectionEffects(uint attacker, uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyDamageReflectionEffects(attacker, defender, damage, damageType);

        public static int ApplyDamageOverTimeTakenModifiers(uint defender, int damage, CombatDamageType damageType) =>
            DamageTaken.ApplyDamageOverTimeTakenModifiers(defender, damage, damageType);

        public static int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType,
            out int targetStatusDamagePercentAdjustment) =>
            DamageTaken.ApplyDamageOverTimeTakenModifiers(defender, damage, damageType, out targetStatusDamagePercentAdjustment);

        public static bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP) =>
            DamageTaken.TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP);

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ApplyDamageTakenEffects() => DamageTaken.ApplyDamageTakenEffects();

        public static void ApplyDamageTakenEffects(uint defender, uint attacker, int damage) =>
            DamageTaken.ApplyDamageTakenEffects(defender, attacker, damage);

        public static void ApplyLowHPDamageTakenEffects(uint defender, int damage) => DamageTaken.ApplyLowHPDamageTakenEffects(defender, damage);

        public static void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage) =>
            DamageTaken.ApplyLowHPGuardEffectFromProtectedTarget(guardRecipient, protectedTarget, damage);

        public static void TrackGuardedHit(uint creature) => DamageTaken.TrackGuardedHit(creature);

        public static void TrackAvoidedAttack(uint creature, uint attacker) => DamageTaken.TrackAvoidedAttack(creature, attacker);

        public static void ApplyMeleeDamageTakenEffects(uint defender, uint attacker) => DamageTaken.ApplyMeleeDamageTakenEffects(defender, attacker);

        public static int ApplyGuardedHitModifiers(uint defender, uint attacker, int damage, CombatDamageType damageType, bool isLandedAttack) =>
            DamageTaken.ApplyGuardedHitModifiers(defender, attacker, damage, damageType, isLandedAttack);

        internal static bool IsHostileAttackSource(uint defender, uint attacker) => DamageTaken.IsHostileAttackSource(defender, attacker);

        /// <inheritdoc cref="ICombatDamageTakenService.GetGuardDamageReductionPercent"/>
        public static int GetGuardDamageReductionPercent(uint defender) => DamageTaken.GetGuardDamageReductionPercent(defender);

        public static bool HasRecentGuardedHit(uint creature, float windowSeconds) => DamageTaken.HasRecentGuardedHit(creature, windowSeconds);

        public static void TrackDeflection(uint creature, DeflectionSource source) => DamageTaken.TrackDeflection(creature, source);

        public static bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds) =>
            DamageTaken.HasRecentDeflection(creature, source, windowSeconds);

        private static int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType) =>
            DamageTaken.ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(creature, skillType);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyIncomingPhysicalToForceDefenseConversion"/>
        public static int ApplyIncomingPhysicalToForceDefenseConversion(
            uint defender,
            CombatDamageType damageType,
            int physicalDefense,
            Func<int> forceDefenseProvider) =>
            DamageTaken.ApplyIncomingPhysicalToForceDefenseConversion(defender, damageType, physicalDefense, forceDefenseProvider);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyIncomingPhysicalToForceConversion"/>
        public static int ApplyIncomingPhysicalToForceConversion(
            uint attacker,
            uint defender,
            CombatDamageType damageType,
            ref int physicalDamage,
            int targetStatusDamagePercentAdjustment = 0,
            Action<Action> convertedDamageScheduler = null) =>
            DamageTaken.ApplyIncomingPhysicalToForceConversion(attacker, defender, damageType, ref physicalDamage, targetStatusDamagePercentAdjustment, convertedDamageScheduler);

        /// <inheritdoc cref="ICombatDamageTakenService.ApplyRangedDeflectionReflection"/>
        public static int ApplyRangedDeflectionReflection(uint defender, uint attacker, SkillType attackerWeaponSkill) =>
            DamageTaken.ApplyRangedDeflectionReflection(defender, attacker, attackerWeaponSkill);

        public static string BuildDeflectingReturnCombatLogMessage(uint observer, uint defender, uint attacker, int damage) =>
            DamageTaken.BuildDeflectingReturnCombatLogMessage(observer, defender, attacker, damage);

        private static void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid) =>
            DamageTaken.ApplyAbilityUsedAttackDeflection(activator, attackDeflectionStatType, durationStatType, deflectionFPRestoreStatType);

        public static void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source) =>
            DamageTaken.ApplyAbilityGrantedAttackDeflectionEffects(activator, source);

        private static void ForgetDamageTakenState(uint creature) => DamageTaken.ForgetDamageTakenState(creature);

        // The ability activation part lives in Domain/Combat/CombatAbilityActivationService.cs. These forwarders keep every member
        // on Combat for its callers.
        private static ICombatAbilityActivationService AbilityActivation => FacadeAccessor.Resolve<ICombatAbilityActivationService>();

        public static void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary) =>
            AbilityActivation.ApplyAbilityActivatedEffects(activator, target, feat, ability, summary);

        public static void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability) =>
            AbilityActivation.TrackQueuedWeaponAbilityUse(activator, ability);

        /// <inheritdoc cref="ICombatAbilityActivationService.ApplyHitPointSpendAbilityEffects"/>
        public static void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0) =>
            AbilityActivation.ApplyHitPointSpendAbilityEffects(activator, hitPointsSpent, hitPointCost);

        private static void ForgetActivationState(uint creature) => AbilityActivation.ForgetActivationState(creature);

        // The ability impact part lives in Domain/Combat/CombatAbilityImpactService.cs. These forwarders keep every member on Combat
        // for its callers.
        private static ICombatAbilityImpactService AbilityImpact => FacadeAccessor.Resolve<ICombatAbilityImpactService>();

        public static int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) =>
            AbilityImpact.GetAbilityImpactBaseDamageBonus(activator, target, ability, skillType);

        public static int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null) =>
            AbilityImpact.GetHighResourceAbilityDamageBonus(activator, ability);

        public static int GetCostlyAbilityDamageBonus(uint activator, AbilityDetail ability, SkillType skillType) =>
            AbilityImpact.GetCostlyAbilityDamageBonus(activator, ability, skillType);

        public static void ApplySuccessfulAbilityImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage,
            bool statusApplied,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            bool firstHostileAbilityHitDamageBonusApplied,
            bool isFirstSuccessfulTarget,
            StatusEffectCategory appliedStatusCategories,
            bool allowsHitProcs = true,
            bool targetWasAttackingAlly = false) =>
            AbilityImpact.ApplySuccessfulAbilityImpactRiders(activator, target, ability, skillType, damageType, damage, statusApplied, primaryStatusEffect, additionalStatusEffects, firstHostileAbilityHitDamageBonusApplied, isFirstSuccessfulTarget, appliedStatusCategories, allowsHitProcs, targetWasAttackingAlly);

        public static void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied) =>
            AbilityImpact.ApplyHostileAbilityPartyBuff(activator, ability, damage, statusApplied);

        public static void ApplySpearDisablerSuppressionRiders(uint activator, uint target) =>
            AbilityImpact.ApplySpearDisablerSuppressionRiders(activator, target);

        /// <inheritdoc cref="ICombatAbilityImpactService.IsTargetAttackingAllyForStatusAppliedRiders"/>
        public static bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target) =>
            AbilityImpact.IsTargetAttackingAllyForStatusAppliedRiders(activator, target);

        /// <inheritdoc cref="ICombatAbilityImpactService.IsAttackingAllyOf"/>
        public static bool IsAttackingAllyOf(uint attacker, uint creature) => AbilityImpact.IsAttackingAllyOf(attacker, creature);

        public static void ApplyStatusAppliedTargetStaminaDrain(uint activator, uint target, StatusEffectCategory appliedCategories) =>
            AbilityImpact.ApplyStatusAppliedTargetStaminaDrain(activator, target, appliedCategories);

        public static void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) =>
            AbilityImpact.ApplyAbilityImpactEffects(activator, summary);

        public static void ApplyAreaHitHaste(uint activator, int impactedTargetCount) =>
            AbilityImpact.ApplyAreaHitHaste(activator, impactedTargetCount);

        private static void ForgetImpactState(uint creature) => AbilityImpact.ForgetImpactState(creature);

        // What is left of Combat lives in Domain/Combat/CombatService.cs: the death, exit and six-second handlers stay here
        // under their names and forward, with the stat-driven tracker refresh and the damage roll.
        private static ICombatService Instance => FacadeAccessor.Resolve<ICombatService>();

        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void ApplyDefeatedEnemyEffects()
        {
            Instance.ApplyDefeatedEnemyEffects();
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearStatTriggerState()
        {
            Instance.ClearStatTriggerState();
        }

        public static void RefreshStatDrivenTrackerEffects(uint creature) => Instance.RefreshStatDrivenTrackerEffects(creature);

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void ClearCombatState()
        {
            Instance.ClearCombatState();
        }

        /// <inheritdoc cref="ICombatService.CalculateDamage"/>
        public static int CalculateDamage(int attackerAttack, int attackerDMG, int attackerStat, int defenderDefense, int defenderStat, int critical, int deltaCap = 0) =>
            Instance.CalculateDamage(attackerAttack, attackerDMG, attackerStat, defenderDefense, defenderStat, critical, deltaCap);
    }
}

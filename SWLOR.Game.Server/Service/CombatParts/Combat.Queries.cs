using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    // The combat queries live in Domain/Combat/CombatQueriesService.cs. These forwarders keep every member on Combat for its callers.
    public static partial class Combat
    {
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
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The read-only questions every attack asks: positions and facing, the weapon and skill an attack or ability uses, status
    /// and stat lookups, and the explicit weapon impact source of each actor.
    /// </summary>
    public interface ICombatQueriesService
    {
        /// <summary>The explicit impact source of the actor's current weapon impact, or null when it has none.</summary>
        WeaponImpactProfile GetWeaponImpactProfile(uint actor);

        /// <summary>Makes <paramref name="profile"/> the actor's current impact source.</summary>
        void SetWeaponImpactProfile(uint actor, WeaponImpactProfile profile);

        /// <summary>Leaves the actor without an explicit impact source.</summary>
        void RemoveWeaponImpactProfile(uint actor);
        /// <summary>The lowest hit chance the attacker's weapon attacks and abilities can have.</summary>
        int GetMinimumHitRate(uint attacker);

        int GetRangedAttackDamageFlatAdjustment(uint attacker, SkillType skillType);

        int GetRangedAttackDefenseIgnorePercentAdjustment(uint attacker, SkillType skillType);

        bool IsMatchingBackAttack(uint attacker, uint defender, SkillType skillType);

        bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType);

        bool IsAttackerBesideTarget(uint attacker, uint defender);

        bool IsAttackerBehindTarget(uint attacker, uint defender);

        uint GetNearestHostileCreatureWithinRange(
            uint source,
            uint origin,
            float radius,
            uint excludedTarget = OBJECT_INVALID);

        bool IsStealthedOrInvisible(uint creature);

        bool HasDamageImmunity(uint defender, CombatDamageType damageType);

        bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance);

        int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType);

        int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType);

        int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender);

        int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(
            uint attacker,
            uint defender,
            SkillType skillType);

        int GetAbilityDamageToSourceAppliedStatusTargetBonus(uint attacker, uint defender);

        bool TargetHasSourceAppliedStatusCategory(
            uint defender,
            uint source,
            StatusEffectCategory category);

        bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category);

        bool AbilityAppliedAnyStatus(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, params Type[] matches);

        bool AbilityAppliedAnyStatusCategory(Type primaryStatusEffect, IEnumerable<Type> additionalStatusEffects, StatusEffectCategory category);

        uint GetAbilityAttackWeapon(uint creature, SkillType skillType);

        uint GetRelevantSkillWeapon(uint creature, SkillType skillType);

        /// <summary>
        /// Sums every source of <paramref name="adjustmentStat"/> whose own
        /// <paramref name="skillTypeStat"/> selector names <paramref name="skillType"/>. Selector
        /// stats hold a <see cref="SkillType"/> id, so they must never be aggregated across
        /// sources before they are compared.
        /// </summary>
        int GetSkillSelectedStatAdjustment(
            uint creature,
            SkillType skillType,
            StatType skillTypeStat,
            StatType adjustmentStat,
            bool unselectedSourcesApplyToEverySkill = false);

        bool IsRangedAbilityLongRangeTarget(
            uint creature,
            uint defender,
            SkillType skillType);

        int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType);

        bool IsTargetNotFacingAttacker(uint attacker, uint defender);

        SkillType GetAbilitySkillType(uint creature, AbilityDetail ability);

        SkillType GetEquippedWeaponSkillType(uint creature);

        bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(
            uint creature,
            AbilityDetail ability);

        bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability);

        bool AbilityMatchesAnyPerkTypeStat(
            uint creature,
            AbilityDetail ability,
            params StatType[] statTypes);

        bool AbilityMatchesPerkCategoryStat(
            uint creature,
            AbilityDetail ability,
            StatType categoryStatType);

        bool AbilityUsedTriggerSkillMatches(
            uint activator,
            AbilityDetail ability,
            SkillType abilitySkillType,
            StatType skillTypeStat,
            StatType? rangedOnlyStat);

        bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType);

        int GetCombatImpactWeaponDamage(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false,
            bool requireMatchingSkill = false,
            int? triggeringWeaponDamage = null,
            bool abilityImpact = false);

        /// <summary>
        /// Whether an ability impact adds weapon DMG. The weapon's damage type applies exactly when its DMG does.
        /// </summary>
        bool IncludesCombatImpactWeaponDamage(SkillType skillType, bool usesQueuedNaturalWeapon);

        CombatDamageType GetCombatImpactWeaponDamageType(
            uint activator,
            SkillType skillType,
            bool usesQueuedNaturalWeapon = false);

        bool HasEquippedWeaponForAbility(uint creature);

        bool CanItemTriggerWeaponAbility(uint item, SkillType abilitySkillType);

        int GetTargetedAbilityAdjustment(
            uint creature,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType);

        int GetPerkAdjustedAbilityScore(uint attacker);

        AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType);

        AbilityType GetQueuedAbilityAccuracyAbilityType(uint creature, SkillType skillType);

        AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType);

        int GetMiscDMGBonus(uint attacker, BaseItem weaponType);

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        int GetPowerAttackDMGBonus(uint attacker);
    }
}

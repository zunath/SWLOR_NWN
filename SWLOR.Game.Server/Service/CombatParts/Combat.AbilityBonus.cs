using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    // The ability and attack bonuses live in Domain/Combat/CombatAbilityBonusService.cs. These forwarders keep every member on Combat for
    // its callers.
    public static partial class Combat
    {
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
    }
}

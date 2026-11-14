using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Ability and attack bonuses for combat: the opening auto-attack, the one-shot next-ability and next-attack
    /// bonuses, the queued weapon ability snapshots, and the adjustments an ability reads from its user's stats.
    /// </summary>
    public interface ICombatAbilityBonusService
    {
        int PrepareOpeningAutoAttack(uint attacker, SkillType skillType);

        void PrepareQueuedWeaponAbilityOpeningAttackAtActivation(uint attacker, SkillType skillType);

        int ConsumeOpeningAutoAttackCriticalDamageAdjustment(uint attacker);

        void ClearOpeningAutoAttackModifiers(uint attacker);

        bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType);

        bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType);

        int GetNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false);

        int ConsumeNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false);

        /// <param name="abilityStatusCategories">Status categories the ability applies; a bonus stored with
        /// a required category (Breaker Reversal: Control) waits for an ability that applies it.</param>
        (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            StatusEffectCategory abilityStatusCategories = StatusEffectCategory.None);

        int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType);

        int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType);

        void StoreQueuedWeaponAbilityCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment);

        void StoreQueuedWeaponAbilityActivationCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment);

        void StoreQueuedWeaponAbilityActivationIdleBonuses(uint creature, SkillType skillType);

        void StoreWeaponAbilityActivationIdleBonuses(
            uint creature,
            bool isIdle,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int defenseIgnorePercent);

        bool HasWeaponAbilityActivationIdleSnapshot(uint creature);

        int GetWeaponAbilityActivationIdleDamageBonus(uint creature);

        int GetWeaponAbilityActivationIdleCriticalRateBonus(uint creature);

        int GetWeaponAbilityActivationIdleDefenseIgnorePercent(uint creature);

        void ClearWeaponAbilityActivationIdleBonuses(uint creature);

        /// <param name="ability">The queued ability. Its status categories add category accuracy
        /// (Scrapper Control: "your control abilities gain Ability Accuracy"), which ability rolls get
        /// in ApplyCombatImpact but the native weapon roll would otherwise miss.</param>
        int GetQueuedWeaponAbilityActivationHitChanceAdjustment(
            uint creature,
            SkillType skillType,
            AbilityDetail ability = null);

        void ClearQueuedWeaponAbilityActivationBonuses(uint creature);

        (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(
            uint creature,
            SkillType skillType);

        void ClearQueuedWeaponAbilityAttemptBonuses(uint creature);

        (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(
            uint creature);

        (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature);

        int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature);

        int ConsumeStatusAppliedNextAttackDamageBonus(uint creature);

        int GetStatusAppliedNextAttackDamageBonus(uint creature);

        void ApplyNextAttackGuardedHitEnmityBonus(
            uint attacker,
            uint defender,
            int enmityBonus);

        void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds);

        void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds);

        int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType);

        int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType);

        int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType);

        int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType);

        int GetAbilityStatusCategoryDamageBonus(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories);

        int GetAbilityStatusCategoryHitChancePercentAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories);

        int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType);

        int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability);

        int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory);

        int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender);

        float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType);

        float ApplyAbilityRecastDelayModifiers(
            uint activator,
            AbilityDetail ability,
            float recastDelaySeconds);

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses.
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>
        int GetAbilityDamageBonus(uint creature, SkillType skill);
    }
}

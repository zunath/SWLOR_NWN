using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    // The ability impact part lives in Domain/Combat/CombatAbilityImpactService.cs. These forwarders keep every member on Combat
    // for its callers.
    public static partial class Combat
    {
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
    }
}

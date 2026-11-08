using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability impact part of combat: the base damage bonuses of an ability impact, the riders of a successful ability
    /// hit on its target, the Spear Disabler suppression riders, the status-applied stamina drain, and the effects of a
    /// finished impact by the number of targets it hit.
    /// </summary>
    public interface ICombatAbilityImpactService
    {
        int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType);

        int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null);

        int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType);

        void ApplySuccessfulAbilityImpactRiders(
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
            bool targetWasAttackingAlly = false);

        void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied);

        void ApplySpearDisablerSuppressionRiders(uint activator, uint target);

        /// <summary>
        /// Whether the target is attacking one of the activator's allies, checked only when the activator
        /// has a rider that needs it.
        /// </summary>
        bool IsTargetAttackingAllyForStatusAppliedRiders(uint activator, uint target);

        /// <summary>
        /// Whether the attacker is attacking a party member of the creature other than the creature itself.
        /// Uses the attacker's current attack target, or, while it is casting or moving, the creature its
        /// enmity table has it attacking.
        /// </summary>
        bool IsAttackingAllyOf(uint attacker, uint creature);

        void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories);

        void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary);

        void ApplyAreaHitHaste(uint activator, int impactedTargetCount);

        void ForgetImpactState(uint creature);
    }
}

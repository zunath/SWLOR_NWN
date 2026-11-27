using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability impact part of combat: the orchestrator of a successful ability hit (the riders of the hit on its target, in order)
    /// and the attacking ally check the hostile impact reads.
    /// </summary>
    public interface ICombatAbilityImpactService
    {
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
    }
}

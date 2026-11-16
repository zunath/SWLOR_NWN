using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The status effects of an ability impact (part 18 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md):
    /// the categories an impact's effects carry, and applying them to a target with the activator's duration adjustment and the
    /// target's resistance. Static callers reach the container's instance through <c>GameServices.AbilityStatusApplication</c>.
    /// </summary>
    public interface IAbilityStatusApplicationService
    {
        /// <summary>The categories of an impact's status effect, its additional effects and the effect its factory makes.</summary>
        StatusEffectCategory GetCombatImpactStatusCategories(
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory);

        /// <summary>
        /// Applies an impact's status effects to a target for the adjusted duration, resisted by
        /// <paramref name="statusResistanceType"/> when it is valid and by the source damage type otherwise. Returns whether any
        /// applied, with the categories of those that did.
        /// </summary>
        bool ApplyCombatImpactStatusEffect(
            uint activator,
            uint target,
            SkillType skillType,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            ResistanceType statusResistanceType,
            CombatDamageType sourceDamageType,
            out StatusEffectCategory appliedStatusCategories);
    }
}

using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The player and NPC damage formulas of an ability hit (part 17 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md): each takes its inputs from <see cref="IAbilityDamageInputService"/> and runs
    /// its damage stages, from the critical rating to the critical hit effects. Each returns the damage the hit deals, or 0
    /// when the impact has no damage.
    /// </summary>
    public interface IAbilityDamageStageService
    {
        /// <summary>The damage of a hit from the activator's attack and the target's defense, for an activator without NPC stat scaling.</summary>
        int CalculateCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0);

        /// <summary>The damage of an NPC's hit with NPC stat scaling: the creature's level and NPC stats, scaled by its ability rank.</summary>
        int CalculateNPCCombatImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int baseDamage,
            CombatDamageType damageType,
            int criticalRatePercentAdjustment = 0,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool canCritical = true,
            int abilityDamagePercentAdjustment = 0,
            int lowHPAbilityDamagePercentAdjustment = 0);
    }
}

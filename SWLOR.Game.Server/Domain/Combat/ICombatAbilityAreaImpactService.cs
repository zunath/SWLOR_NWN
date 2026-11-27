using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The area abilities and the damage riders of an ability hit: the area pulse, the fragmentation, the ricochet, the splash, the bleed
    /// refresh and spread, and the area ability hit sequence.
    /// </summary>
    public interface ICombatAbilityAreaImpactService : ICombatCreatureStateOwner
    {
        void ApplyAbilityDamageRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType,
            int damage);

        void ApplyAreaAbilityPulse(
            uint activator,
            uint target,
            AbilityDetail ability,
            bool isFirstSuccessfulTarget);

        void ApplyAreaAbilityTargetHitSequenceEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType);

        /// <summary>Clears the area ability hit sequences that name the creature as the activator or the target.</summary>
        void ForgetAreaAbilityTargetHitSequences(uint creature);
    }
}

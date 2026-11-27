using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The effects an ability hit has at hit time (costly ability rewards, the near-target status of a ranged hit, the same-target hit
    /// count reward, the next auto-attack with no delay) and the effects of a finished impact by the number of targets it hit.
    /// </summary>
    public interface ICombatAbilityHitRidersService : ICombatCreatureStateOwner
    {
        void ApplyCostlyAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType);

        void ApplyRangedAbilityHitNearTargetEffects(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType);

        void ApplySameTargetHostileAbilityHitEffects(
            uint activator,
            uint target,
            AbilityDetail ability);

        void ApplyHostileAbilityHitNextAutoAttackNoDelay(
            uint activator,
            AbilityDetail ability);

        void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary);

        void ApplyAreaHitHaste(uint activator, int impactedTargetCount);

        /// <summary>Clears the same-target hit counts that name the creature as the activator or the target.</summary>
        void ForgetSameTargetHitState(uint creature);
    }
}

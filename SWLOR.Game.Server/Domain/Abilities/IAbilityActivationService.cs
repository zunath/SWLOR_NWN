using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The activation checks of an ability (part 6 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md): whether
    /// a creature can activate a perk feat, and why the most recent check refused. Static callers reach the container's
    /// instance through <c>GameServices.AbilityActivation</c>.
    /// </summary>
    public interface IAbilityActivationService
    {
        /// <summary>
        /// The reason the most recent CanUseAbility call returned false. Player-facing feedback
        /// goes through SendMessageToPC, which is invisible for NPC activators - the in-engine
        /// test harness reads this to report WHY an activation was rejected. Cleared at the
        /// start of every check.
        /// </summary>
        string GetLastActivationDenialReason();

        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation);
    }
}

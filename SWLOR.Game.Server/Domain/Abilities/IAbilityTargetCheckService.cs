using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The checks of an activation's target (part 7 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md):
    /// line of sight, the target, the target location, range and hostility, the technique, the requirements, the definition's
    /// own validation, hostile area line of sight and the recast. <see cref="IAbilityActivationService.CanUseAbility"/> calls
    /// them between its own checks, in their original order. Each returns the denial of the first check that fails, or empty.
    /// </summary>
    public interface IAbilityTargetCheckService
    {
        /// <summary>A target the ability needs must be in line of sight. Returns the denial, or empty.</summary>
        string ValidateLineOfSight(uint activator, uint target, AbilityDetail ability);

        /// <summary>
        /// The target, the target location, range, hostility, the technique, the requirements, the definition's own
        /// validation, hostile area line of sight and the recast, in that order. Returns the denial of the first that fails,
        /// or empty. A refusal for the recast also shows the cooldown text over the activator, as it did before the split.
        /// </summary>
        string ValidateTarget(
            uint activator,
            uint target,
            AbilityDetail ability,
            int effectivePerkLevel,
            Location targetLocation);
    }
}

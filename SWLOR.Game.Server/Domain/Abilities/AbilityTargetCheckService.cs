using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The checks of an activation's target (part 7 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>CanUseAbility</c> in <c>AbilityService</c> unchanged apart from the split of that method: each check that
    /// called <c>Deny(reason)</c> returns the reason instead, and <see cref="AbilityActivationService.CanUseAbility"/> denies
    /// with it, so the order of the checks and their messages do not change.
    /// </summary>
    public class AbilityTargetCheckService : IAbilityTargetCheckService
    {
        private readonly ICombatImpactGeometryService _geometry;
        private readonly IMeleeTechniqueService _meleeTechnique;
        private readonly IRecastService _recast;
        private readonly IPlayerFeedbackService _playerFeedback;

        public AbilityTargetCheckService(
            ICombatImpactGeometryService geometry,
            IMeleeTechniqueService meleeTechnique,
            IRecastService recast,
            IPlayerFeedbackService playerFeedback)
        {
            _geometry = geometry;
            _meleeTechnique = meleeTechnique;
            _recast = recast;
            _playerFeedback = playerFeedback;
        }

        public string ValidateLineOfSight(uint activator, uint target, AbilityDetail ability)
        {
            // Must be within line of sight.
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                !_geometry.HasAbilityLineOfSight(activator, target))
            {
                return "You cannot see your target.";
            }

            return string.Empty;
        }

        public string ValidateTarget(
            uint activator,
            uint target,
            AbilityDetail ability,
            int effectivePerkLevel,
            Location targetLocation)
        {
            // Target check.
            if (ability.RequiresTarget && !GetIsObjectValid(target))
            {
                return "A target is required.";
            }

            // Aimed areas use the feat's location cursor. They must not use RequiresTarget,
            // because empty-ground casts have no target object and object range/hostility checks
            // do not describe a selected location or direction.
            if (ability.RequiresLocationTarget)
            {
                var targetArea = GetAreaFromLocation(targetLocation);
                if (!GetIsObjectValid(targetArea) || targetArea != GetArea(activator))
                {
                    return "A target location in your current area is required.";
                }

                if (ability.HasExplicitMaxRange &&
                    GetDistanceBetweenLocations(GetLocation(activator), targetLocation) > ability.MaxRange)
                {
                    return "You are out of range.  This ability has a range of " + ability.MaxRange + " meters.";
                }
            }

            // Object range check. Location-targeted areas are validated separately above.
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                GetDistanceBetween(activator, target) > ability.MaxRange)
            {
                return "You are out of range.  This ability has a range of " + ability.MaxRange + " meters.";
            }

            // Hostility check
            if (ability.RequiresTarget &&
                GetIsObjectValid(target) &&
                !GetIsReactionTypeHostile(target, activator) &&
                ability.IsHostileAbility)
            {
                return "You may only use this ability on enemies.";
            }

            // Perk-specific requirement checks
            var techniqueError = _meleeTechnique.ValidateActivation(activator, ability, target);
            if (!string.IsNullOrWhiteSpace(techniqueError))
                return techniqueError;

            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator, ability);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    return requirementError;
                }
            }

            // Perk-specific custom validation logic.
            var customValidationResult = ability.CustomValidation == null ? string.Empty : ability.CustomValidation(activator, target, effectivePerkLevel, targetLocation);
            if (!string.IsNullOrWhiteSpace(customValidationResult))
            {
                return customValidationResult;
            }

            var areaLineOfSightError = ValidateHostileAreaLineOfSight(activator, target, targetLocation, ability);
            if (!string.IsNullOrWhiteSpace(areaLineOfSightError))
            {
                return areaLineOfSightError;
            }

            // Check if ability is on a recast timer still.
            var (isOnRecast, timeToWait) = _recast.IsOnRecastDelay(activator, ability.RecastGroup);
            if (isOnRecast)
            {
                _playerFeedback.ShowCooldownFloatingText(activator, ability.Name, timeToWait);
                return $"This ability can be used in {timeToWait}.";
            }

            return string.Empty;
        }

        private string ValidateHostileAreaLineOfSight(
            uint activator,
            uint target,
            Location targetLocation,
            AbilityDetail ability)
        {
            var targeting = ability.Targeting;
            if (targeting == null ||
                !targeting.Flags.HasFlag(AbilityTargetingFlags.HarmsEnemies) ||
                !_geometry.TryGetCombatImpactShape(targeting.Shape, out var shape))
            {
                return string.Empty;
            }

            var creatures = _geometry.GetHostileCreaturesInCombatImpactShape(
                    activator,
                    target,
                    targetLocation,
                    shape,
                    targeting.ResolveSizeX(activator, true),
                    targeting.ResolveSizeY(),
                    targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf),
                    targeting.Flags.HasFlag(AbilityTargetingFlags.BackOffsetOrigin))
                .ToList();

            if (creatures.Count <= 0 ||
                creatures.Any(creature => _geometry.HasAbilityLineOfSight(activator, creature)))
            {
                return string.Empty;
            }

            return "You cannot see any enemies in the target area.";
        }
    }
}

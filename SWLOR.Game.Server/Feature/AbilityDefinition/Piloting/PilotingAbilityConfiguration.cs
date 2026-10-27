using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// The shared setup of a Piloting style ability that fires the ship's weapons (plan section 6). Every space
    /// ability is cast from the ship with no activation delay, pays the pilot's own STM and recast, and fires
    /// through <see cref="SpaceAbilities"/> from its impact action.
    /// </summary>
    public static class PilotingAbilityConfiguration
    {
        /// <summary>
        /// Sets up an ability that fires at one target: a hostile single-target cast whose validation turns the
        /// ship away before the STM is paid when no weapon of the family can fire at the target. The caller names
        /// the ability, sets its level and recast, and passes the impact.
        /// </summary>
        public static AbilityBuilder ConfigureTargetedShot(
            AbilityBuilder ability,
            int stamina,
            SpaceWeaponFilter filter,
            AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasActivationDelay(0f)
                .RequirementStamina(stamina)
                .BreaksStealth()
                .HasCustomValidation((activator, target, _, _) => SpaceAbilities.GetFireError(activator, target, filter))
                .HasImpactAction(impact);
        }

        /// <summary>
        /// Sets up a self-centered burst: an area ability that fires at the enemy ships within
        /// <paramref name="radius"/> of the pilot's ship, with no cursor.
        /// </summary>
        public static AbilityBuilder ConfigureShipBurst(
            AbilityBuilder ability,
            int stamina,
            Spell targetingSpell,
            float radius,
            SpaceWeaponFilter filter,
            AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .HasActivationDelay(0f)
                .RequirementStamina(stamina)
                .BreaksStealth()
                .HasTargetingSphere(
                    targetingSpell,
                    radius,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasCustomValidation((activator, _, _, _) => GetBurstError(activator, radius, filter))
                .HasImpactAction(impact);
        }

        /// <summary>Why a burst around the pilot's ship cannot fire, or an empty string when it can.</summary>
        public static string GetBurstError(uint activator, float radius, SpaceWeaponFilter filter)
        {
            var enemies = Space.GetEnemyShipsNear(activator, GetLocation(activator), radius);
            return enemies.Count == 0
                ? "There are no enemy ships near your ship."
                : SpaceAbilities.GetFireError(activator, enemies[0], filter);
        }

        /// <summary>Tells the pilot why an impact could not fire. The ability's validation normally catches it first.</summary>
        public static void ReportFailure(uint activator, IEnumerable<SpaceFireResult> results)
        {
            var failure = results.FirstOrDefault(x => !x.Fired);
            if (failure != null && !string.IsNullOrWhiteSpace(failure.Error))
                SendMessageToPC(activator, failure.Error);
        }
    }
}

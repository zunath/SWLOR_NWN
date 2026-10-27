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
        /// The longest reach of any launcher module (the Missile Launchers' 50 m). An area ability aimed at a ship
        /// states it as its range, because an area aimed at an object must declare one; the launcher that fires
        /// still limits what the blast can reach.
        /// </summary>
        public const float LauncherRange = 50f;

        /// <summary>
        /// How far a goad reaches: the Armor Provoke's 15 m scaled to space. A goad fires no weapon, so it states
        /// its own range.
        /// </summary>
        public static readonly float GoadRange = SpaceRadii.FromGround(15f);

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

        /// <summary>
        /// Sets up an area ability that bursts around the target ship: a hostile area cast whose cursor picks the
        /// enemy ship and whose blast reaches the ships within <paramref name="radius"/> of it. Its range is
        /// <paramref name="maxRange"/>; the weapon the ship fires still decides what it can reach.
        /// </summary>
        public static AbilityBuilder ConfigureTargetedBurst(
            AbilityBuilder ability,
            int stamina,
            Spell targetingSpell,
            float radius,
            float maxRange,
            SpaceWeaponFilter filter,
            AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .RequiresTarget()
                .HasMaxRange(maxRange)
                .HasActivationDelay(0f)
                .RequirementStamina(stamina)
                .BreaksStealth()
                .HasTargetingSphere(targetingSpell, radius, AbilityTargetingFlags.HarmsEnemies)
                .HasCustomValidation((activator, target, _, _) => SpaceAbilities.GetFireError(activator, target, filter))
                .HasImpactAction(impact);
        }

        /// <summary>
        /// Sets up an ability that arms a rider: the ship's next landed weapon hit carries its effect. It has no
        /// target and no cursor, and its validation turns a ship with no weapon away before the STM is paid.
        /// </summary>
        public static AbilityBuilder ConfigureRider(AbilityBuilder ability, int stamina, AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .HasActivationDelay(0f)
                .RequirementStamina(stamina)
                .HasCustomValidation((activator, _, _, _) => GetRiderError(activator))
                .HasImpactAction(impact);
        }

        /// <summary>
        /// Sets up a goad at one enemy ship: a hostile single-target cast that fires no weapon and costs no STM.
        /// </summary>
        public static AbilityBuilder ConfigureGoad(AbilityBuilder ability, AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasMaxRange(GoadRange)
                .HasActivationDelay(0f)
                .BreaksStealth()
                .HasCustomValidation((activator, target, _, _) => GetGoadError(activator, target))
                .HasImpactAction(impact);
        }

        /// <summary>
        /// Sets up a goad at an enemy ship and the enemy ships near it: a hostile area cast whose cursor picks the
        /// ship, fires no weapon and costs no STM.
        /// </summary>
        public static AbilityBuilder ConfigureAreaGoad(
            AbilityBuilder ability,
            Spell targetingSpell,
            float radius,
            AbilityImpactAction impact)
        {
            return ability
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .RequiresTarget()
                .HasMaxRange(GoadRange)
                .HasActivationDelay(0f)
                .BreaksStealth()
                .HasTargetingSphere(targetingSpell, radius, AbilityTargetingFlags.HarmsEnemies)
                .HasCustomValidation((activator, target, _, _) => GetGoadError(activator, target))
                .HasImpactAction(impact);
        }

        /// <summary>Why a rider cannot be armed, or an empty string when it can: the ship needs a weapon to hit with.</summary>
        public static string GetRiderError(uint activator)
        {
            return SpaceAbilities.GetLongestWeaponRange(activator) > 0f
                ? string.Empty
                : "Your ship has no weapon to hit with.";
        }

        /// <summary>Why a goad cannot be cast at the target, or an empty string when it can.</summary>
        public static string GetGoadError(uint activator, uint target)
        {
            return GetIsObjectValid(target) && target != activator && Space.IsShipInSpace(target) && GetIsEnemy(target, activator)
                ? string.Empty
                : "Your target must be an enemy ship.";
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

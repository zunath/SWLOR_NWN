using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ThrowLightsaberAbilityDefinition : IAbilityListDefinition
    {
        private const float RangeMeters = 15f;
        private const float PathWidthMeters = 2.5f;
        private const float PathBoundaryToleranceMeters = 0.001f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ThrowLightsaber1(builder);
            ThrowLightsaber2(builder);
            ThrowLightsaber3(builder);

            return builder.Build();
        }

        private static void ThrowLightsaber1(AbilityBuilder builder)
        {
            ConfigureThrowLightsaber(builder, FeatType.ThrowLightsaber1, Spell.ThrowLightsaber1, "Throw Lightsaber I", 1, 10, 2, 1, 1);
        }

        private static void ThrowLightsaber2(AbilityBuilder builder)
        {
            ConfigureThrowLightsaber(builder, FeatType.ThrowLightsaber2, Spell.ThrowLightsaber2, "Throw Lightsaber II", 2, 20, 3, 1, 2);
        }

        private static void ThrowLightsaber3(AbilityBuilder builder)
        {
            ConfigureThrowLightsaber(builder, FeatType.ThrowLightsaber3, Spell.ThrowLightsaber3, "Throw Lightsaber III", 3, 30, 4, 2, 3);
        }

        private static void ConfigureThrowLightsaber(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int fp,
            int stamina,
            int maxTargets)
        {
            builder
                .Create(feat, PerkType.ThrowLightsaber)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ThrowLightsaber, 12f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.SaberThrow)
                .DisplaysVisualEffectWhenActivating()
                .IsAreaAbility()
                .HasTargetingLine(
                    spell,
                    RangeMeters,
                    PathWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasMaxRange(RangeMeters)
                .RequiresTarget()
                .HasCustomValidation(ValidateWeapon)
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyThrowLightsaber(activator, target, targetLocation, name, baseDamage, maxTargets))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp)
                .RequirementStamina(stamina);
        }

        private static void ApplyThrowLightsaber(
            uint activator,
            uint target,
            Location targetLocation,
            string abilityName,
            int baseDamage,
            int maxTargets)
        {
            PlayThrowLightsaberAnimation(activator, target, targetLocation);

            var foundTarget = false;
            foreach (var hitTarget in GetPathTargets(activator, target, targetLocation, maxTargets))
            {
                foundTarget = true;
                Ability.ApplyCombatImpact(
                    activator,
                    hitTarget,
                    GetLocation(hitTarget),
                    SkillType.Force,
                    baseDamage,
                    0,
                    null,
                    false,
                    Array.Empty<Type>(),
                    damageType: CombatDamageType.Physical,
                    targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                    baseDamageAdjustment: GetEquippedWeaponDamageAdjustment(activator),
                    playImpactAnimation: false);
            }

            if (!foundTarget)
            {
                Messaging.SendMessageNearbyToPlayers(
                    activator,
                    receiver => Combat.BuildAbilityNoTargetCombatLogMessage(
                        receiver,
                        activator,
                        abilityName),
                    60f);
            }
        }

        private static void PlayThrowLightsaberAnimation(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target) && target != activator)
            {
                BiowarePosition.TurnToFaceObject(target, activator);
            }
            else if (GetIsObjectValid(GetAreaFromLocation(targetLocation)))
            {
                BiowarePosition.TurnToFaceLocation(targetLocation, activator);
            }

            AssignCommand(activator, () => ActionPlayAnimation(Animation.SaberThrow, 2));
        }

        private static IEnumerable<uint> GetPathTargets(
            uint activator,
            uint target,
            Location targetLocation,
            int maxTargets)
        {
            return AbilityTargeting.GetHostileTargetsNearLocation(
                activator,
                GetLocation(activator),
                RangeMeters,
                maxTargets,
                target,
                candidate => candidate == target || IsTargetAlongPath(activator, target, targetLocation, candidate));
        }

        private static bool IsTargetAlongPath(uint activator, uint target, Location targetLocation, uint candidate)
        {
            var origin = GetPosition(activator);
            var destination = GetIsObjectValid(target)
                ? GetPosition(target)
                : GetPositionFromLocation(targetLocation);
            return IsPositionAlongPath(origin, destination, GetPosition(candidate));
        }

        private static bool IsPositionAlongPath(
            NumericsVector3 origin,
            NumericsVector3 destination,
            NumericsVector3 candidate)
        {
            var path = destination - origin;
            var pathLength = path.Length();
            if (pathLength <= 0.01f)
                return false;

            pathLength = Math.Min(pathLength, RangeMeters);
            var direction = NumericsVector3.Normalize(path);
            var toCandidate = candidate - origin;
            var distanceAlongPath = NumericsVector3.Dot(toCandidate, direction);
            if (distanceAlongPath < -PathBoundaryToleranceMeters ||
                distanceAlongPath > pathLength + PathBoundaryToleranceMeters)
                return false;

            var closestPoint = origin + direction * distanceAlongPath;
            var lateralDistance = (candidate - closestPoint).Length();
            return lateralDistance <= PathWidthMeters * 0.5f + PathBoundaryToleranceMeters;
        }

        private static Func<uint, int> GetEquippedWeaponDamageAdjustment(uint activator)
        {
            var weapon = GetEquippedWeapon(activator);
            var damage = GetIsObjectValid(weapon)
                ? Item.GetDMG(weapon)
                : 0;

            return damage <= 0
                ? null
                : _ => damage;
        }

        private static uint GetEquippedWeapon(uint activator)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            if (Item.IsBaseItemType(rightHand, Item.WeaponBaseItemTypes))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            if (Item.IsBaseItemType(leftHand, Item.WeaponBaseItemTypes))
                return leftHand;

            return OBJECT_INVALID;
        }

        private static string ValidateWeapon(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            return GetIsObjectValid(GetEquippedWeapon(activator))
                ? string.Empty
                : "An equipped weapon is required.";
        }
    }
}

using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class ClusterGrenadeAbilityDefinition : IAbilityListDefinition
    {
        private const int GrenadeCount = 3;
        private const float SmallBlastRadius = 2f;
        private const float ClusterBlastCenterOffset = SmallBlastRadius * 0.5f;
        private const float ClusterTargetingRadius = SmallBlastRadius + ClusterBlastCenterOffset;
        private const float ClusterBlastAngleSpacingRadians = (float)(Math.PI * 2d / GrenadeCount);
        private const float DegreesToRadians = (float)(Math.PI / 180d);

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ClusterGrenade1(builder);

            return builder.Build();
        }

        private static void ClusterGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ClusterGrenade1, PerkType.ClusterGrenade)
                .Name("Cluster Grenade")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ClusterGrenade, 24f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.ClusterGrenade1,
                    ClusterTargetingRadius,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(ClusterGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5)
                .RequirementItem("explosives");
        }

        private static void ClusterGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = GetImpactLocation(activator, target, targetLocation);
            var blastLocations = GetClusterBlastLocations(activator, location);
            var blastRadius = DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, SmallBlastRadius);
            var hasAnyTargets = HasHostileTargetInAnyBlast(activator, blastLocations, blastRadius);

            for (var grenadeIndex = 0; grenadeIndex < GrenadeCount; grenadeIndex++)
            {
                ApplyClusterBlast(
                    activator,
                    blastLocations[grenadeIndex],
                    !hasAnyTargets && grenadeIndex == 0);
            }
        }

        private static void ApplyClusterBlast(uint activator, Location targetLocation, bool sendsNoTargetMessage)
        {
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Fnf_Fireball),
                targetLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                OBJECT_INVALID,
                targetLocation,
                SkillType.Devices,
                18,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, SmallBlastRadius),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.None,
                sendsNoTargetMessage: sendsNoTargetMessage);
        }

        private static Location[] GetClusterBlastLocations(uint activator, Location impactLocation)
        {
            var area = GetAreaFromLocation(impactLocation);
            var center = GetPositionFromLocation(impactLocation);
            var facingRadians = GetClusterFacingRadians(activator, impactLocation);
            var locations = new Location[GrenadeCount];

            for (var grenadeIndex = 0; grenadeIndex < GrenadeCount; grenadeIndex++)
            {
                var angle = facingRadians + ClusterBlastAngleSpacingRadians * grenadeIndex;
                var position = Vector3(
                    center.X + (float)Math.Cos(angle) * ClusterBlastCenterOffset,
                    center.Y + (float)Math.Sin(angle) * ClusterBlastCenterOffset,
                    center.Z);

                locations[grenadeIndex] = Location(area, position, 0f);
            }

            return locations;
        }

        private static float GetClusterFacingRadians(uint activator, Location impactLocation)
        {
            var origin = GetPosition(activator);
            var destination = GetPositionFromLocation(impactLocation);
            var delta = destination - origin;

            if (Math.Abs(delta.X) <= 0.01f && Math.Abs(delta.Y) <= 0.01f)
                return GetFacing(activator) * DegreesToRadians;

            return (float)Math.Atan2(delta.Y, delta.X);
        }

        private static bool HasHostileTargetInAnyBlast(uint activator, IEnumerable<Location> blastLocations, float blastRadius)
        {
            foreach (var blastLocation in blastLocations)
            {
                var creature = GetFirstObjectInShape(Shape.Sphere, blastRadius, blastLocation, true, ObjectType.Creature);
                while (GetIsObjectValid(creature))
                {
                    if (creature != activator && GetIsReactionTypeHostile(creature, activator))
                        return true;

                    creature = GetNextObjectInShape(Shape.Sphere, blastRadius, blastLocation, true, ObjectType.Creature);
                }
            }

            return false;
        }

        private static Location GetImpactLocation(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetLocation(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

    }
}

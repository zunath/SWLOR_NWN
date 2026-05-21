using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class ClusterGrenadeAbilityDefinition : IAbilityListDefinition
    {
        private const int GrenadeCount = 3;
        private const float TargetSearchRadius = 5f;
        private const float SmallBlastRadius = 2f;

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
                .HasRecastDelay(RecastGroup.ClusterGrenade, 45f)
                .SkillType(SkillType.Devices)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.ClusterGrenade1,
                    SmallBlastRadius,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyGrenadeRadiusBonus)
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
            var grenadeTargets = GetClusterGrenadeTargets(activator, location)
                .Take(GrenadeCount)
                .ToList();

            if (grenadeTargets.Count <= 0)
            {
                ApplyClusterBlast(activator, OBJECT_INVALID, location);
                return;
            }

            foreach (var grenadeTarget in grenadeTargets)
            {
                ApplyClusterBlast(activator, grenadeTarget, GetLocation(grenadeTarget));
            }
        }

        private static void ApplyClusterBlast(uint activator, uint target, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                18,
                12,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, SmallBlastRadius),
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Fnf_Fireball);
        }

        private static IEnumerable<uint> GetClusterGrenadeTargets(uint activator, Location location)
        {
            var nth = 1;
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            while (GetIsObjectValid(creature) &&
                   GetDistanceBetweenLocations(location, GetLocation(creature)) <= TargetSearchRadius)
            {
                if (creature != activator && GetIsReactionTypeHostile(creature, activator))
                    yield return creature;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }
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

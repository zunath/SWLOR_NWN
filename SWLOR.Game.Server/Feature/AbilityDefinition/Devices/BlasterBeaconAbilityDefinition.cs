using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
    public sealed class BlasterBeaconAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BlasterBeacon1(builder);
            BlasterBeacon2(builder);
            BlasterBeacon3(builder);

            return builder.Build();
        }

        private static void BlasterBeacon1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BlasterBeacon1, PerkType.BlasterBeacon)
                .Name("Blaster Beacon I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.BlasterBeacon, 45f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(BlasterBeacon1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void BlasterBeacon2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BlasterBeacon2, PerkType.BlasterBeacon)
                .Name("Blaster Beacon II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.BlasterBeacon, 45f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(BlasterBeacon2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void BlasterBeacon3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BlasterBeacon3, PerkType.BlasterBeacon)
                .Name("Blaster Beacon III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.BlasterBeacon, 45f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(BlasterBeacon3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void BlasterBeacon1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                10,
                0,
                null,
                12f,
                18f,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void BlasterBeacon2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                14,
                0,
                null,
                12f,
                21f,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void BlasterBeacon3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                18,
                0,
                null,
                14f,
                24f,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

    }
}

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
    public sealed class KillzoneBeaconAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            KillzoneBeacon1(builder);

            return builder.Build();
        }

        private static void KillzoneBeacon1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.KillzoneBeacon1, PerkType.KillzoneBeacon)
                .Name("Killzone Beacon")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.KillzoneBeacon, 120f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(KillzoneBeacon1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void KillzoneBeacon1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                location,
                SkillType.Devices,
                22,
                0,
                null,
                12f,
                18f,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Com_Chunk_Red_Small);

            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                location,
                SkillType.Devices,
                14,
                6,
                typeof(ShockStatusEffect),
                12f,
                18f,
                CombatDamageType.Electrical,
                VisualEffect.Vfx_Com_Hit_Electrical,
                VisualEffect.Vfx_Fnf_Electric_Explosion);
        }

    }
}

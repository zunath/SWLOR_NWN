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
    public sealed class RayshieldScreenAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RayshieldScreen1(builder);
            RayshieldScreen2(builder);

            return builder.Build();
        }

        private static void RayshieldScreen1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RayshieldScreen1, PerkType.RayshieldScreen)
                .Name("Rayshield Screen I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.RayshieldScreen, 75f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(RayshieldScreen1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void RayshieldScreen2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RayshieldScreen2, PerkType.RayshieldScreen)
                .Name("Rayshield Screen II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.RayshieldScreen, 75f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(RayshieldScreen2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void RayshieldScreen1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRayshieldScreen(activator, target, targetLocation, typeof(RayshieldScreen1StatusEffect), 15f);
        }

        private static void RayshieldScreen2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRayshieldScreen(activator, target, targetLocation, typeof(RayshieldScreen2StatusEffect), 18f);
        }

        private static void ApplyRayshieldScreen(uint activator, uint target, Location targetLocation, Type statusEffect, float duration)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            AbilityAreaEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                4f,
                duration,
                statusEffect,
                VisualEffect.Vfx_Imp_Ac_Bonus,
                areaMarkerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue,
                areaMarkerVisualEffectScale: 2f);
        }

    }
}

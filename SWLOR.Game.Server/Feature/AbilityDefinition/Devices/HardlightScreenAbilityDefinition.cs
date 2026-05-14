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
    public sealed class HardlightScreenAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            HardlightScreen1(builder);
            HardlightScreen2(builder);

            return builder.Build();
        }

        private static void HardlightScreen1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.HardlightScreen1, PerkType.HardlightScreen)
                .Name("Hardlight Screen I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.HardlightScreen, 75f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(HardlightScreen1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void HardlightScreen2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.HardlightScreen2, PerkType.HardlightScreen)
                .Name("Hardlight Screen II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.HardlightScreen, 75f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(HardlightScreen2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void HardlightScreen1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyHardlightScreen(activator, target, targetLocation, typeof(HardlightScreen1StatusEffect), 15f);
        }

        private static void HardlightScreen2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyHardlightScreen(activator, target, targetLocation, typeof(HardlightScreen2StatusEffect), 18f);
        }

        private static void ApplyHardlightScreen(uint activator, uint target, Location targetLocation, Type statusEffect, float duration)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            DeviceAbilityEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                4f,
                duration,
                statusEffect,
                VisualEffect.Vfx_Imp_Ac_Bonus);
        }

    }
}

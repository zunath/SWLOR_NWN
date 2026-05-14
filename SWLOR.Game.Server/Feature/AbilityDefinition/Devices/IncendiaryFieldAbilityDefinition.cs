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
    public sealed class IncendiaryFieldAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IncendiaryField1(builder);
            IncendiaryField2(builder);
            IncendiaryField3(builder);

            return builder.Build();
        }

        private static void IncendiaryField1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IncendiaryField1, PerkType.IncendiaryField)
                .Name("Incendiary Field I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.IncendiaryField, 60f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void IncendiaryField2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IncendiaryField2, PerkType.IncendiaryField)
                .Name("Incendiary Field II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.IncendiaryField, 60f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void IncendiaryField3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.IncendiaryField3, PerkType.IncendiaryField)
                .Name("Incendiary Field III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.IncendiaryField, 60f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField3ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void IncendiaryField1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                10,
                0,
                null,
                5f,
                12f,
                CombatDamageType.Fire,
                VisualEffect.Vfx_Com_Hit_Fire,
                VisualEffect.Fnf_Fireball);
        }

        private static void IncendiaryField2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                14,
                0,
                null,
                5f,
                15f,
                CombatDamageType.Fire,
                VisualEffect.Vfx_Com_Hit_Fire,
                VisualEffect.Fnf_Fireball);
        }

        private static void IncendiaryField3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                18,
                0,
                null,
                5f,
                18f,
                CombatDamageType.Fire,
                VisualEffect.Vfx_Com_Hit_Fire,
                VisualEffect.Fnf_Fireball);
        }

    }
}

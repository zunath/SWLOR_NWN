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
        private const VisualEffect IncendiaryFieldTargetVisualEffect = VisualEffect.Vfx_Imp_Flame_S;
        private const VisualEffect IncendiaryFieldMarkerVisualEffect = VisualEffect.Vfx_Dur_Aura_Fire;

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
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField1ImpactAction)
                .HasTargetingSphere(
                    Spell.IncendiaryField1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
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
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField2ImpactAction)
                .HasTargetingSphere(
                    Spell.IncendiaryField2,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
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
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(IncendiaryField3ImpactAction)
                .HasTargetingSphere(
                    Spell.IncendiaryField3,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
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
                8,
                0,
                null,
                5f,
                12f,
                CombatDamageType.Fire,
                targetVisualEffect: IncendiaryFieldTargetVisualEffect,
                markerVisualEffect: IncendiaryFieldMarkerVisualEffect,
                markerVisualEffectScale: 2f);
        }

        private static void IncendiaryField2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                12,
                0,
                null,
                5f,
                15f,
                CombatDamageType.Fire,
                targetVisualEffect: IncendiaryFieldTargetVisualEffect,
                markerVisualEffect: IncendiaryFieldMarkerVisualEffect,
                markerVisualEffectScale: 2f);
        }

        private static void IncendiaryField3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                16,
                0,
                null,
                5f,
                18f,
                CombatDamageType.Fire,
                targetVisualEffect: IncendiaryFieldTargetVisualEffect,
                markerVisualEffect: IncendiaryFieldMarkerVisualEffect,
                markerVisualEffectScale: 2f);
        }

    }
}

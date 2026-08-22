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
        private const float IncendiaryFieldRadiusMeters = 5f;
        private const float IncendiaryFieldDurationSeconds = 30f;

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
                .HasRecastDelay(RecastGroup.IncendiaryField, 30f)
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
                .HasRecastDelay(RecastGroup.IncendiaryField, 30f)
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
                .HasRecastDelay(RecastGroup.IncendiaryField, 30f)
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
            DeployIncendiaryField(activator, target, targetLocation, 8);
        }

        private static void IncendiaryField2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeployIncendiaryField(activator, target, targetLocation, 12);
        }

        private static void IncendiaryField3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeployIncendiaryField(activator, target, targetLocation, 16);
        }

        private static void DeployIncendiaryField(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            // Script-free custom AoE row: renders the live-server incendiary grenade's fire fog
            // cloud visual only, without the base game fire cloud enter/heartbeat spell effects.
            // Damage comes exclusively from the scheduled pulses below.
            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectAreaOfEffect(AreaOfEffect.IncendiaryFieldCloud),
                location,
                IncendiaryFieldDurationSeconds);

            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                location,
                SkillType.Devices,
                baseDamage,
                0,
                null,
                IncendiaryFieldRadiusMeters,
                IncendiaryFieldDurationSeconds,
                CombatDamageType.Fire,
                targetVisualEffect: IncendiaryFieldTargetVisualEffect);
        }

    }
}

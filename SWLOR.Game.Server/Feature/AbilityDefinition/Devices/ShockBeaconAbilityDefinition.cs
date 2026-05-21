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
    public sealed class ShockBeaconAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ShockBeacon1(builder);
            ShockBeacon2(builder);

            return builder.Build();
        }

        private static void ShockBeacon1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ShockBeacon1, PerkType.ShockBeacon)
                .Name("Shock Beacon I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ShockBeacon, 75f)
                .SkillType(SkillType.Devices)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(ShockBeacon1ImpactAction)
                .HasTargetingSphere(
                    Spell.ShockBeacon1,
                    10f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void ShockBeacon2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ShockBeacon2, PerkType.ShockBeacon)
                .Name("Shock Beacon II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ShockBeacon, 75f)
                .SkillType(SkillType.Devices)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(ShockBeacon2ImpactAction)
                .HasTargetingSphere(
                    Spell.ShockBeacon2,
                    12f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void ShockBeacon1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                10,
                6,
                typeof(ShockStatusEffect),
                10f,
                15f,
                CombatDamageType.Electrical,
                VisualEffect.Vfx_Com_Hit_Electrical,
                VisualEffect.Vfx_Fnf_Electric_Explosion,
                markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue);
        }

        private static void ShockBeacon2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DeviceAbilityEffects.ScheduleSingleHostilePulses(
                activator,
                AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                SkillType.Devices,
                14,
                6,
                typeof(ShockStatusEffect),
                12f,
                18f,
                CombatDamageType.Electrical,
                VisualEffect.Vfx_Com_Hit_Electrical,
                VisualEffect.Vfx_Fnf_Electric_Explosion,
                markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue);
        }

    }
}

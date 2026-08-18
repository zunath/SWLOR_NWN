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
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Devices)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasImpactAction(KillzoneBeacon1ImpactAction)
                .HasTargetingSphere(
                    Spell.KillzoneBeacon1,
                    12f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBeaconPulseRangeBonus)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void KillzoneBeacon1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                location,
                SkillType.Devices,
                16,
                0,
                null,
                12f,
                CapstoneAbility.ActiveDurationSeconds,
                CombatDamageType.Physical,
                VisualEffect.Vfx_Com_Chunk_Red_Small,
                markerVisualEffect: VisualEffect.Vfx_Dur_Aura_Pulse_Red_Blue,
                markerVisualEffectScale: 4.8f,
                appliesBeaconPulseBonuses: true);

            DeviceAbilityEffects.ScheduleAreaHostilePulses(
                activator,
                location,
                SkillType.Devices,
                16,
                45,
                typeof(ShockStatusEffect),
                12f,
                CapstoneAbility.ActiveDurationSeconds,
                CombatDamageType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_M,
                VisualEffect.Vfx_Imp_Mirv_Electric,
                appliesBeaconPulseBonuses: true,
                showAreaIndicator: false);
        }

    }
}

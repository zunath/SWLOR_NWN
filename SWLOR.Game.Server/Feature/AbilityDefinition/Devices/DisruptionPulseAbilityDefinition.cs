using System;
using System.Collections.Generic;
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
    public sealed class DisruptionPulseAbilityDefinition : IAbilityListDefinition
    {
        private const float RangeMeters = 12f;
        private const float RadiusMeters = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.DisruptionPulse1, PerkType.DisruptionPulse)
                .Name("Disruption Pulse")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.DisruptionPulse, 24f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasMaxRange(RangeMeters)
                .HasCustomValidation(ValidateTargetingRange)
                .HasTargetingSphere(
                    Spell.DisruptionPulse1,
                    RadiusMeters,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4)
                .RequirementItem("explosives");

            return builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            var radius = DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, RadiusMeters);
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                impactLocation,
                SkillType.Devices,
                18,
                12,
                typeof(DisruptionPulseStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.4f,
                radius,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                statusResistanceType: ResistanceType.Disruption,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Electric_Explosion,
                afterImpactAction: _ => DeviceAbilityEffects.ApplyDiagnosticSweep(activator, impactLocation, radius),
                alwaysApplyAreaVisualEffect: true);
        }

        private static string ValidateTargetingRange(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            if (GetDistanceBetweenLocations(GetLocation(activator), location) <= RangeMeters)
                return string.Empty;

            return $"You are out of range. This ability has a range of {RangeMeters} meters.";
        }
    }
}

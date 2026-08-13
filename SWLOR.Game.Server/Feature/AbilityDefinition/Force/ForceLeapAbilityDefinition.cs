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
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceLeapAbilityDefinition : IAbilityListDefinition
    {
        private const float LeapAnimationSpeed = 2.0f;
        private const float LeapAnimationDurationSeconds = 1.0f;
        private const float ArrivalDistanceMeters = 1.5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceLeap1(builder);
            ForceLeap2(builder);

            return builder.Build();
        }

        private static void ForceLeap1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLeap1, PerkType.ForceLeap)
                .Name("Force Leap I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceLeap, 18f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .PlaysSoundOnImpact("ksfx_frc_speed")
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceLeap1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void ForceLeap2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceLeap2, PerkType.ForceLeap)
                .Name("Force Leap II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ForceLeap, 18f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .PlaysSoundOnImpact("ksfx_frc_speed")
                .IsSingleTargetAbility()
                .HasMaxRange(18f)
                .RequiresTarget()
                .HasImpactAction(ForceLeap2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void ForceLeap1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                10,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void ForceLeap2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                18,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative);
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }

        private static void LeapAndInterrupt(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            var destination = GetLeapDestination(activator, target);
            AssignCommand(target, () => ClearAllActions());
            AssignCommand(activator, () =>
            {
                ClearAllActions();
                ActionPlayAnimation(Animation.ForceLeap, LeapAnimationSpeed, LeapAnimationDurationSeconds);
                ActionJumpToLocation(destination);
                ActionDoCommand(() => SetFacingPoint(GetPosition(target)));
            });
        }

        private static Location GetLeapDestination(uint activator, uint target)
        {
            var activatorPosition = GetPosition(activator);
            var targetPosition = GetPosition(target);
            var offsetX = activatorPosition.X - targetPosition.X;
            var offsetY = activatorPosition.Y - targetPosition.Y;
            var distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);

            if (distance < 0.01)
            {
                var targetFacingRadians = GetFacing(target) * Math.PI / 180.0;
                offsetX = -(float)Math.Cos(targetFacingRadians);
                offsetY = -(float)Math.Sin(targetFacingRadians);
                distance = 1.0;
            }

            var destinationPosition = Vector3(
                targetPosition.X + offsetX / (float)distance * ArrivalDistanceMeters,
                targetPosition.Y + offsetY / (float)distance * ArrivalDistanceMeters,
                targetPosition.Z);

            return Location(GetArea(target), destinationPosition, GetFacing(activator));
        }
    }
}

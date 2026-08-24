using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class PounceAbilityDefinition : IAbilityListDefinition
    {
        private const float LeapAnimationSpeed = 2.0f;
        private const float LeapAnimationDurationSeconds = 1.0f;
        private const float ArrivalDistanceMeters = 1.5f;
        private const float MaxRangeMeters = 15.0f;
        private const float PounceOpeningDistanceMeters = 3.0f;
        private const int Pounce1Damage = 14;
        private const int Pounce2Damage = 24;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Pounce1(builder);
            Pounce2(builder);

            return builder.Build();
        }

        private static void Pounce1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pounce1, PerkType.Pounce)
                .Name("Pounce I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Pounce, 18f)
                .SkillType(SkillType.BeastMastery)
                .HasAIScore(context => GetPounceScore(context, 1))
                .IsSingleTargetAbility()
                .HasMaxRange(MaxRangeMeters)
                .RequiresTarget()
                .HasImpactAction(Pounce1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Pounce2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Pounce2, PerkType.Pounce)
                .Name("Pounce II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Pounce, 18f)
                .SkillType(SkillType.BeastMastery)
                .HasAIScore(context => GetPounceScore(context, 2))
                .IsSingleTargetAbility()
                .HasMaxRange(MaxRangeMeters)
                .RequiresTarget()
                .HasImpactAction(Pounce2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void Pounce1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                Pounce1Damage,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void Pounce2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LeapAndInterrupt(activator, target);

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                Pounce2Damage,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small);
        }

        private static void LeapAndInterrupt(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return;

            AssignCommand(target, () => ClearAllActions());
            AssignCommand(activator, () =>
            {
                ClearAllActions();
                ActionPlayAnimation(Animation.ForceLeap, LeapAnimationSpeed, LeapAnimationDurationSeconds);
                ActionDoCommand(() =>
                {
                    if (!GetIsObjectValid(target))
                        return;

                    var destination = GetLeapDestination(activator, target);
                    JumpToLocation(destination);
                    SetFacingPoint(GetPosition(target));
                });
            });
        }

        private static int GetPounceScore(AIContext context, int abilityLevel)
        {
            var target = context.EvaluatedTarget;
            return GetIsObjectValid(target) &&
                   GetDistanceBetween(context.Self, target) > PounceOpeningDistanceMeters
                ? AIScoreBand.CrowdControl + abilityLevel
                : AIScoreBand.SingleTargetDamage + abilityLevel;
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

using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class ShadowStepAbilityDefinition : IAbilityListDefinition
    {
        private const string EvasionModifierGroup = "SHADOW_STEP_EVASION";
        private const float EvasionDurationSeconds = 30f;
        private const float ArrivalDistanceMeters = 1.5f;
        private const float StunDurationSeconds = 2f;
        private const float ArrivalToleranceMeters = 2f;
        private const float ArrivalCheckIntervalSeconds = 0.1f;
        private const int ArrivalChecks = 10;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ShadowStep(builder, FeatType.ShadowStep1, "Shadow Step I", 1, 10, 10, false);
            ShadowStep(builder, FeatType.ShadowStep2, "Shadow Step II", 2, 14, 15, true);

            return builder.Build();
        }

        private static void ShadowStep(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            int evasionPercent,
            bool cleansesMovementImpairing)
        {
            builder
                .Create(feat, PerkType.ShadowStep)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_ShadowStep)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ShadowStep, 24f)
                .SkillType(SkillType.Espionage)
                .IsSingleTargetAbility()
                .HasMaxRange(10f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyShadowStep(activator, target, evasionPercent, cleansesMovementImpairing))
                .IsCastedAbility()
                .PreservesNativeAnimationChoreography()
                .IsHostileAbility()
                .RequirementStamina(stamina);
        }

        private static void ApplyShadowStep(
            uint activator,
            uint target,
            int evasionPercent,
            bool cleansesMovementImpairing)
        {
            if (!GetIsObjectValid(target))
                return;

            var targetPosition = GetPosition(target);
            var targetFacing = GetFacing(target);
            var facingRadians = targetFacing * Math.PI / 180.0;
            var behind = Vector3(
                targetPosition.X - (float)Math.Cos(facingRadians) * ArrivalDistanceMeters,
                targetPosition.Y - (float)Math.Sin(facingRadians) * ArrivalDistanceMeters,
                targetPosition.Z);
            var destination = Location(GetArea(target), behind, targetFacing);

            var playArrivalVisual = Ability.CaptureSuccessfulImpactVisualEffect(activator);
            var arrivalHandled = false;

            void CompleteArrival()
            {
                if (arrivalHandled)
                    return;
                arrivalHandled = true;

                if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) ||
                    GetIsDead(activator) || GetIsDead(target) || GetArea(activator) != GetArea(target))
                    return;

                AssignCommand(activator, () => SetFacingPoint(GetPosition(target)));
                if (HasArrived(activator, destination))
                {
                    playArrivalVisual(activator);
                    if (GetIsReactionTypeHostile(target, activator))
                    {
                        // A target fighting the activator turns toward the new position before the
                        // arrival runs. Turn it back so the stun holds it facing away.
                        SetFacing(targetFacing, target);
                        StatusEffect.ApplyStatusEffect(activator, target, typeof(StunnedStatusEffect),
                            StunDurationSeconds, CombatDamageType.Physical);
                    }
                }
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), activator);
            }

            // A move order given right after the cast clears the queued arrival action, so the
            // landing is also watched outside the action queue.
            void WatchForArrival(int remainingChecks)
            {
                if (arrivalHandled || !GetIsObjectValid(activator))
                    return;

                if (HasArrived(activator, destination))
                {
                    CompleteArrival();
                    return;
                }

                if (remainingChecks > 1)
                    DelayCommand(ArrivalCheckIntervalSeconds, () => WatchForArrival(remainingChecks - 1));
            }

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), activator);
            AssignCommand(activator, () =>
            {
                // Stop the current attack so it cannot block the teleport and arrival actions.
                ClearAllActions();
                ActionJumpToLocation(destination);
                ActionDoCommand(CompleteArrival);
                DelayCommand(ArrivalCheckIntervalSeconds, () => WatchForArrival(ArrivalChecks));
            });

            TemporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                EvasionDurationSeconds,
                EvasionModifierGroup);

            if (cleansesMovementImpairing)
            {
                StatusEffect.RemoveStatusEffectsWithNegativeStat(activator, StatType.MovementSpeedPercentAdjustment);
            }
        }

        private static bool HasArrived(uint activator, Location destination)
        {
            return GetArea(activator) == GetAreaFromLocation(destination) &&
                   GetDistanceBetweenLocations(GetLocation(activator), destination) < ArrivalToleranceMeters;
        }
    }
}

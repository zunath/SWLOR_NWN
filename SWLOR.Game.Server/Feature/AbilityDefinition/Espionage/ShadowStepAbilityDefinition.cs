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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), activator);
            AssignCommand(activator, () =>
            {
                // An ongoing attack can block queued jumps indefinitely. Complete the
                // teleport and arrival effects before the ability pipeline resumes combat.
                ClearAllActions();
                JumpToLocation(destination);
                if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) ||
                    GetIsDead(activator) || GetIsDead(target) || GetArea(activator) != GetArea(target))
                    return;

                SetFacingPoint(GetPosition(target));
                if (GetArea(activator) == GetAreaFromLocation(destination) &&
                    GetDistanceBetweenLocations(GetLocation(activator), destination) < 2f)
                {
                    playArrivalVisual(activator);
                    if (GetIsReactionTypeHostile(target, activator))
                    {
                        // Keep an engaged target facing away when the stun freezes it.
                        SetFacing(targetFacing, target);
                        StatusEffect.ApplyStatusEffect(activator, target, typeof(StunnedStatusEffect),
                            StunDurationSeconds, CombatDamageType.Physical);
                    }
                }
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Unsummon), activator);
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
    }
}

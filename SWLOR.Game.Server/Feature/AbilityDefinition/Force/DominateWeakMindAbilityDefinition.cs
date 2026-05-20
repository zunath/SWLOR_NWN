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
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class DominateWeakMindAbilityDefinition : IAbilityListDefinition
    {
        private const int DominateWeakMindDurationSeconds = 8;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DominateWeakMind1(builder);

            return builder.Build();
        }

        private static void DominateWeakMind1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DominateWeakMind1, PerkType.DominateWeakMind)
                .Name("Dominate Weak Mind")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DominateWeakMind, 90f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((_, target, _, _) => ValidateNonMechanicalTarget(target))
                .HasImpactAction(DominateWeakMind1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void DominateWeakMind1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                afterSuccessfulHit: hitTarget => ApplyDominateWeakMindEffects(activator, hitTarget));
        }

        private static void ApplyDominateWeakMindEffects(uint activator, uint target)
        {
            var duration = GetAdjustedDurationSeconds(activator);
            var statusApplied = HasMindStatusImmunity(target)
                ? ApplyAccuracyFallback(activator, target, duration)
                : StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(FoggyMindStatusEffect),
                    duration,
                    ResistanceType.Mind);

            if (!statusApplied)
            {
                statusApplied = ApplyAccuracyFallback(activator, target, duration);
            }

            if (statusApplied)
            {
                ApplyEffectToObject(
                    DurationType.Instant,
                    EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Negative),
                    target);
            }
        }

        private static int GetAdjustedDurationSeconds(uint activator)
        {
            var adjustment = Combat.GetAbilityStatusDurationPercentAdjustment(activator, PerkType.DominateWeakMind);
            if (adjustment == 0)
                return DominateWeakMindDurationSeconds;

            return Math.Max(
                1,
                DominateWeakMindDurationSeconds + (int)Math.Ceiling(DominateWeakMindDurationSeconds * (adjustment / 100f)));
        }

        private static bool ApplyAccuracyFallback(uint activator, uint target, int duration)
        {
            return StatusEffect.ApplyStatusEffect(
                activator,
                target,
                typeof(DominateWeakMind1StatusEffect),
                duration);
        }

        private static bool HasMindStatusImmunity(uint target)
        {
            return Stat.GetStatAdjustment(target, StatType.MindStatusImmunity) > 0;
        }

        private static bool IsNonMechanical(uint target)
        {
            var racialType = GetRacialType(target);
            return racialType != RacialType.Construct &&
                   racialType != RacialType.Robot &&
                   racialType != RacialType.Droid;
        }

        private static string ValidateNonMechanicalTarget(uint target)
        {
            return IsNonMechanical(target)
                ? string.Empty
                : "This ability cannot affect mechanical targets.";
        }

    }
}

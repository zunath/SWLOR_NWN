using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class RousingShoutAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RousingShout1(builder);
            RousingShout2(builder);
            RousingShout3(builder);

            return builder.Build();
        }

        private static void RousingShout1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RousingShout1, PerkType.RousingShout)
                .Name("Rousing Shout I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.RousingShout, 45f)
                .SkillType(SkillType.Leadership)
                .HasMaxRange(LeadershipAbilityRange.CommandTarget)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation(ValidateRousingShoutTarget)
                .HasImpactAction(RousingShout1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }

        private static void RousingShout2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RousingShout2, PerkType.RousingShout)
                .Name("Rousing Shout II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.RousingShout, 45f)
                .SkillType(SkillType.Leadership)
                .HasMaxRange(LeadershipAbilityRange.CommandTarget)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation(ValidateRousingShoutTarget)
                .HasImpactAction(RousingShout2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void RousingShout3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RousingShout3, PerkType.RousingShout)
                .Name("Rousing Shout III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .HasRecastDelay(RecastGroup.RousingShout, 45f)
                .SkillType(SkillType.Leadership)
                .HasMaxRange(LeadershipAbilityRange.CommandTarget)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation(ValidateRousingShoutTarget)
                .HasImpactAction(RousingShout3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void RousingShout1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRousingShout(activator, target, 10, 13, typeof(RousingShout1StatusEffect), 30f);
        }

        private static void RousingShout2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRousingShout(activator, target, 15, 19, typeof(RousingShout2StatusEffect), 30f);
        }

        private static void RousingShout3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRousingShout(activator, target, 20, 25, typeof(RousingShout3StatusEffect), 30f);
        }

        private static void ApplyRousingShout(
            uint activator,
            uint target,
            int temporaryHPPercent,
            int temporaryHPCap,
            Type lowHPStatusEffect,
            float durationSeconds)
        {
            if (!CanRousingShoutAffectTarget(activator, target))
                return;

            durationSeconds = LeadershipAbilityEffects.ApplyFieldStewardCommandDurationBonus(activator, durationSeconds);
            ApplyTemporaryHP(
                target,
                AbilityEffectScaling.ScaleValueBySourceSocial(activator, temporaryHPPercent, temporaryHPCap),
                durationSeconds);

            if (IsTargetInDanger(target))
            {
                StatusEffect.ApplyStatusEffect(activator, target, lowHPStatusEffect, durationSeconds);
            }

            LeadershipAbilityEffects.ApplyTriageProtocol(activator, target, durationSeconds);
            LeadershipAbilityEffects.ApplyBolsterResolve(activator, durationSeconds);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Good_Help), target);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Leadership, 2);
        }

        private static bool CanRousingShoutAffectTarget(uint activator, uint target)
        {
            var error = ValidateRousingShoutTarget(activator, target, 0, null);
            if (string.IsNullOrWhiteSpace(error))
                return true;

            SendMessageToPC(activator, error);
            return false;
        }

        private static string ValidateRousingShoutTarget(
            uint activator,
            uint target,
            int effectivePerkLevel,
            Location targetLocation)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Creature)
                return "Rousing Shout requires a living ally.";

            if (GetIsDead(target) || GetCurrentHitPoints(target) <= 0)
                return "Rousing Shout cannot affect the dead.";

            if (GetIsReactionTypeHostile(target, activator))
                return "Rousing Shout can only affect allies.";

            return string.Empty;
        }

        private static bool IsTargetInDanger(uint target)
        {
            return GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * 0.35f;
        }

        private static void ApplyTemporaryHP(uint target, int percent, float durationSeconds)
        {
            TemporaryHitPointEffects.ApplyFlat(
                target,
                "ROUSING_SHOUT",
                GameMath.PercentOf(GetMaxHitPoints(target), percent),
                durationSeconds);
        }

    }
}

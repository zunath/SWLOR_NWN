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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class InnervateAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Innervate1(builder);
            Innervate2(builder);
            Innervate3(builder);

            return builder.Build();
        }

        private static void Innervate1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate1, PerkType.Innervate)
                .Name("Innervate I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void Innervate2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate2, PerkType.Innervate)
                .Name("Innervate II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void Innervate3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Innervate3, PerkType.Innervate)
                .Name("Innervate III")
                .Level(3)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FireForgetSalute)
                .HasRecastDelay(RecastGroup.Innervate, 12f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Innervate3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void Innervate1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, 6);
            }
        }

        private static void Innervate2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, 10);
            }
        }

        private static void Innervate3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                HealPercent(activator, friendly, 14);
            }
        }


        private static void HealPercent(uint activator, uint target, int percent)
        {
            var amount = GameMath.PercentOf(GetMaxHitPoints(target), percent);
            amount = Stat.ApplyOutgoingAbilityHealingAdjustment(activator, amount);
            amount = Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(activator, amount);
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }
    }
}

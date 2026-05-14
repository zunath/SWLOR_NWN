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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class RenewalAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Renewal1(builder);
            Renewal2(builder);
            Renewal3(builder);

            return builder.Build();
        }

        private static void Renewal1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal1, PerkType.Renewal)
                .Name("Renewal I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Renewal, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void Renewal2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal2, PerkType.Renewal)
                .Name("Renewal II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Renewal, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void Renewal3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Renewal3, PerkType.Renewal)
                .Name("Renewal III")
                .Level(3)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Renewal, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Renewal3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void Renewal1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal I", 12f);
        }

        private static void Renewal2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal II", 18f);
        }

        private static void Renewal3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyRenewal(activator, target, "Renewal III", 24f);
        }

        private static void ApplyRenewal(uint activator, uint target, string name, float totalPercent)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            StatusEffect.ApplyStatusEffect(
                activator,
                friendly,
                new RegenerativeHealingStatusEffect(name, totalPercent, 6),
                18f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
        }

        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)
        {
            var ability = skill switch
            {
                SkillType.Leadership => AbilityType.Social,
                SkillType.Devices => AbilityType.Perception,
                SkillType.BeastMastery => AbilityType.Might,
                _ => AbilityType.Willpower
            };
            var baseAmount = PercentOf(GetMaxHitPoints(target), percent);
            var amount = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(baseAmount, GetAbilityScore(activator, ability));
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}

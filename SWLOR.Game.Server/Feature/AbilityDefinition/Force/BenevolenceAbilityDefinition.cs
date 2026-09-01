using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class BenevolenceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Benevolence1(builder);
            Benevolence2(builder);
            Benevolence3(builder);

            return builder.Build();
        }

        private static void Benevolence1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Benevolence1, PerkType.Benevolence)
                .Name("Benevolence I")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Benevolence1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(3);
        }

        private static void Benevolence2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Benevolence2, PerkType.Benevolence)
                .Name("Benevolence II")
                .Level(2)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Benevolence2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void Benevolence3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Benevolence3, PerkType.Benevolence)
                .Name("Benevolence III")
                .Level(3)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .IsHealingAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Benevolence3ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void Benevolence1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBenevolence(activator, target, 8);
        }

        private static void Benevolence2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBenevolence(activator, target, 14);
        }

        private static void Benevolence3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBenevolence(activator, target, 20);
        }

        private static void ApplyBenevolence(uint activator, uint target, int percent)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target);
            var targetWasBelowHalfHP = ForceControlHealingEffects.IsBelowHalfHP(friendly);
            var multiplier = friendly == activator ? 1f : 1.25f;
            AbilityEffectScaling.ApplyActivatedScaledHeal(activator, friendly, percent, multiplier: multiplier);
            ForceControlHealingEffects.ApplyRestorativeControlPower(activator, friendly, targetWasBelowHalfHP);
        }
    }
}

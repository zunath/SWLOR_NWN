using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceHealAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceHeal1(builder);
            ForceHeal2(builder);
            ForceHeal3(builder);
            ForceHeal4(builder);
            ForceHeal5(builder);

            return builder.Build();
        }

        private static void ForceHeal1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal1, PerkType.ForceHeal)
                .Name("Force Heal I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(3)
                .HasMaxRange(15)
                .IsConcentrationAbility(StatusEffectType.ForceHeal1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal2, PerkType.ForceHeal)
                .Name("Force Heal II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .HasMaxRange(15)
                .IsConcentrationAbility(StatusEffectType.ForceHeal2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal3, PerkType.ForceHeal)
                .Name("Force Heal III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(5)
                .HasMaxRange(15)
                .IsConcentrationAbility(StatusEffectType.ForceHeal3)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal4, PerkType.ForceHeal)
                .Name("Force Heal IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .HasMaxRange(15)
                .IsConcentrationAbility(StatusEffectType.ForceHeal4)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal5(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal5, PerkType.ForceHeal)
                .Name("Force Heal V")
                .Level(5)
                .HasRecastDelay(RecastGroup.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(7)
                .HasMaxRange(15)
                .IsConcentrationAbility(StatusEffectType.ForceHeal5)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
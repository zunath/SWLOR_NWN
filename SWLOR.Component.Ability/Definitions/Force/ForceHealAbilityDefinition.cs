using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.Force
{
    public class ForceHealAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceHeal1(builder);
            ForceHeal2(builder);
            ForceHeal3(builder);
            ForceHeal4(builder);
            ForceHeal5(builder);

            return builder.Build();
        }

        private void ForceHeal1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal1, PerkType.ForceHeal)
                .Name("Force Heal I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(3)
                .HasMaxRange(15)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceHeal2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal2, PerkType.ForceHeal)
                .Name("Force Heal II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .HasMaxRange(15)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceHeal3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal3, PerkType.ForceHeal)
                .Name("Force Heal III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(5)
                .HasMaxRange(15)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceHeal4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal4, PerkType.ForceHeal)
                .Name("Force Heal IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .HasMaxRange(15)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceHeal5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal5, PerkType.ForceHeal)
                .Name("Force Heal V")
                .Level(5)
                .HasRecastDelay(RecastGroupType.ForceHeal, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(7)
                .HasMaxRange(15)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceDrainAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceDrain1(builder);
            ForceDrain2(builder);
            ForceDrain3(builder);
            ForceDrain4(builder);
            ForceDrain5(builder);

            return builder.Build();
        }

        private void ForceDrain1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain1, PerkType.ForceDrain)
                .Name("Force Drain I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain2, PerkType.ForceDrain)
                .Name("Force Drain II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain3, PerkType.ForceDrain)
                .Name("Force Drain III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain4, PerkType.ForceDrain)
                .Name("Force Drain IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain5, PerkType.ForceDrain)
                .Name("Force Drain V")
                .HasRecastDelay(RecastGroupType.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .IsHostileAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

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
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.ForceDrain1)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain2, PerkType.ForceDrain)
                .Name("Force Drain II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.ForceDrain2)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain3, PerkType.ForceDrain)
                .Name("Force Drain III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ForceDrain3)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain4, PerkType.ForceDrain)
                .Name("Force Drain IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.ForceDrain4)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void ForceDrain5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain5, PerkType.ForceDrain)
                .Name("Force Drain V")
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ForceDrain5)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

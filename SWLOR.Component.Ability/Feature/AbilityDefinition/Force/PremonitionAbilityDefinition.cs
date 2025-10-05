using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class PremonitionAbilityDefinition: IAbilityListDefinition
    {

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Premonition1(builder);
            Premonition2(builder);

            return builder.Build();
        }

        private void Premonition1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Premonition1, PerkType.Premonition)
                .Name("Premonition I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Premonition, 60f)
                .RequirementFP(4)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void Premonition2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Premonition2, PerkType.Premonition)
                .Name("Premonition II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Premonition, 60f)
                .RequirementFP(6)
                .IsCastedAbility()
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

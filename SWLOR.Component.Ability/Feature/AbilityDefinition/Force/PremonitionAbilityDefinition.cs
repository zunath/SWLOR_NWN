using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

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
                .HasRecastDelay(RecastGroup.Premonition, 60f)
                .RequirementFP(4)
                .IsCastedAbility()
                .IsConcentrationAbility(StatusEffectType.Premonition1)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private void Premonition2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Premonition2, PerkType.Premonition)
                .Name("Premonition II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Premonition, 60f)
                .RequirementFP(6)
                .IsCastedAbility()
                .IsConcentrationAbility(StatusEffectType.Premonition2)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.Force
{
    public class ComprehendSpeechAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ComprehendSpeech1(builder);
            ComprehendSpeech2(builder);
            ComprehendSpeech3(builder);
            ComprehendSpeech4(builder);

            return builder.Build();
        }

        private void ComprehendSpeech1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech1, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.ComprehendSpeech, 12f)
                .RequirementFP(2)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech2, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.ComprehendSpeech, 12f)
                .RequirementFP(3)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech3, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.ComprehendSpeech, 12f)
                .RequirementFP(4)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech4, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech IV")
                .Level(4)
                .HasRecastDelay(RecastGroupType.ComprehendSpeech, 12f)
                .RequirementFP(5)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

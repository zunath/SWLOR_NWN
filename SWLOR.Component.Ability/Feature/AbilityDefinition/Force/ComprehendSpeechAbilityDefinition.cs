//using Random = SWLOR.Game.Server.Service.Random;

using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
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
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 12f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech1)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech2, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 12f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech2)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech3, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 12f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech3)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private void ComprehendSpeech4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech4, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 12f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech4)
                .UsesAnimation(AnimationType.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}

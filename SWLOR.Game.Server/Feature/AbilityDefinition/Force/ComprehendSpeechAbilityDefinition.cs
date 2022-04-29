//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ComprehendSpeechAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ComprehendSpeech1(builder);
            ComprehendSpeech2(builder);
            ComprehendSpeech3(builder);
            ComprehendSpeech4(builder);

            return builder.Build();
        }

        private static void ComprehendSpeech1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech1, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech I")
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 60f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void ComprehendSpeech2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech2, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech II")
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 60f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void ComprehendSpeech3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech3, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech III")
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 60f)
                .RequirementFP(8)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech3)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void ComprehendSpeech4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ComprehendSpeech4, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech IV")
                .HasRecastDelay(RecastGroup.ComprehendSpeech, 60f)
                .RequirementFP(10)
                .IsConcentrationAbility(StatusEffectType.ComprehendSpeech4)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}
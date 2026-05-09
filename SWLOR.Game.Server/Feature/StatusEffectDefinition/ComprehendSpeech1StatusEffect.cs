using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ComprehendSpeech1StatusEffect : ComprehendSpeechStatusEffectBase
    {
        public override string Name => "Comprehend Speech I";
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech2StatusEffect),
            typeof(ComprehendSpeech3StatusEffect),
            typeof(ComprehendSpeech4StatusEffect)
        };

        public ComprehendSpeech1StatusEffect()
            : base(5)
        {
        }
    }
}

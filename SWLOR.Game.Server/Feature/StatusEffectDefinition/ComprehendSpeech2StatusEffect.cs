using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ComprehendSpeech2StatusEffect : ComprehendSpeechStatusEffectBase
    {
        public override string Name => "Comprehend Speech II";
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech3StatusEffect),
            typeof(ComprehendSpeech4StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech1StatusEffect)
        };

        public ComprehendSpeech2StatusEffect()
            : base(10)
        {
        }
    }
}

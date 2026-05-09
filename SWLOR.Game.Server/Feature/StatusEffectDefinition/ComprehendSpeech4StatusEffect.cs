using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ComprehendSpeech4StatusEffect : ComprehendSpeechStatusEffectBase
    {
        public override string Name => "Comprehend Speech IV";
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech1StatusEffect),
            typeof(ComprehendSpeech2StatusEffect),
            typeof(ComprehendSpeech3StatusEffect)
        };

        public ComprehendSpeech4StatusEffect()
            : base(20)
        {
        }
    }
}

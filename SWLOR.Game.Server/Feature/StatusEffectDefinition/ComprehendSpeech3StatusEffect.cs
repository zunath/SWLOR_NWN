using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ComprehendSpeech3StatusEffect : ComprehendSpeechStatusEffectBase
    {
        public override string Name => "Comprehend Speech III";
        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech4StatusEffect)
        };
        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ComprehendSpeech1StatusEffect),
            typeof(ComprehendSpeech2StatusEffect)
        };

        public ComprehendSpeech3StatusEffect()
            : base(15)
        {
        }
    }
}

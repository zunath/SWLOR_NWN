using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition.AdrenalStim
{
    public class AdrenalStim3StatusEffect : AdrenalStimStatusEffectBase
    {
        protected override int Level => 3;
        protected override string EffectName => "Adrenal Stim III";

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(AdrenalStim1StatusEffect),
            typeof(AdrenalStim2StatusEffect)
        };
    }
}

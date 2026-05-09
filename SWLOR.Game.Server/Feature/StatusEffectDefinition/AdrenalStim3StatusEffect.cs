using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim3StatusEffect : AdrenalStimStatusEffectBase
    {
        protected override int Level => 3;
        protected override string EffectName => "Adrenal Stim III";
        protected override Type WillpowerPenaltyStatusEffectClass => typeof(AdrenalStim3WillpowerPenaltyStatusEffect);

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(AdrenalStim1StatusEffect),
            typeof(AdrenalStim2StatusEffect)
        };
    }
}

using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim1StatusEffect : AdrenalStimStatusEffectBase
    {
        protected override int Level => 1;
        protected override string EffectName => "Adrenal Stim I";
        protected override Type WillpowerPenaltyStatusEffectClass => typeof(AdrenalStim1WillpowerPenaltyStatusEffect);

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(AdrenalStim2StatusEffect),
            typeof(AdrenalStim3StatusEffect)
        };
    }
}

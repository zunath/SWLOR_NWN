using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim2StatusEffect : AdrenalStimStatusEffectBase
    {
        protected override int Level => 2;
        protected override string EffectName => "Adrenal Stim II";
        protected override Type WillpowerPenaltyStatusEffectClass => typeof(AdrenalStim2WillpowerPenaltyStatusEffect);

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(AdrenalStim3StatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(AdrenalStim1StatusEffect)
        };
    }
}

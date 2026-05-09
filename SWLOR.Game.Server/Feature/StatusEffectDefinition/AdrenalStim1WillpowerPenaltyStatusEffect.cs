using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim1WillpowerPenaltyStatusEffect : AdrenalStimWillpowerPenaltyStatusEffectBase
    {
        protected override int Penalty => 2;

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(AdrenalStim2WillpowerPenaltyStatusEffect),
            typeof(AdrenalStim3WillpowerPenaltyStatusEffect)
        };
    }
}

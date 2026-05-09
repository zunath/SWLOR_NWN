using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim2WillpowerPenaltyStatusEffect : AdrenalStimWillpowerPenaltyStatusEffectBase
    {
        protected override int Penalty => 4;

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(AdrenalStim3WillpowerPenaltyStatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(AdrenalStim1WillpowerPenaltyStatusEffect)
        };
    }
}

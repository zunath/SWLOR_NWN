using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStim3WillpowerPenaltyStatusEffect : AdrenalStimWillpowerPenaltyStatusEffectBase
    {
        protected override int Penalty => 6;

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(AdrenalStim1WillpowerPenaltyStatusEffect),
            typeof(AdrenalStim2WillpowerPenaltyStatusEffect)
        };
    }
}

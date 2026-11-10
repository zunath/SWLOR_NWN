using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class MovementSpeedStatusEffectBase : StatusEffectBase
    {
        protected abstract int MovementSpeedPercentAdjustment { get; }
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = MovementSpeedPercentAdjustment;
        }
    }
}

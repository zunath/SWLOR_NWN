using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class MovementSpeedStatusEffectBase : StatusEffectBase
    {
        protected abstract int MovementSpeedPercentAdjustment { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = MovementSpeedPercentAdjustment;
        }
    }
}

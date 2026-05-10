using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;

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

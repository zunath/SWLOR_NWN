using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class BurstOfSpeedStatusEffectBase : StatusEffectBase
    {
        protected abstract int MovementSpeedPercentAdjustment { get; }
        protected abstract int DefenseBonus { get; }

        public override string Name => "Burst of Speed";
        public override EffectIconType Icon => EffectIconType.MovementSpeedIncrease;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = MovementSpeedPercentAdjustment;
            StatGroup.Stats[StatType.Defense] = DefenseBonus;
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SweepingGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Sweeping Guard";
        public override EffectIconType Icon => EffectIconType.ACIncrease;

        public SweepingGuardStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
        }
    }
}

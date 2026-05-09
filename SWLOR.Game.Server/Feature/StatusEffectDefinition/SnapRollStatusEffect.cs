using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SnapRollStatusEffect : StatusEffectBase
    {
        public override string Name => "Snap Roll";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public SnapRollStatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 25;
        }

    }
}

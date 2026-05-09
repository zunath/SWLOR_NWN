using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CenteringStatusEffect : StatusEffectBase
    {
        public override string Name => "Centering";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public CenteringStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 10;
        }

    }
}

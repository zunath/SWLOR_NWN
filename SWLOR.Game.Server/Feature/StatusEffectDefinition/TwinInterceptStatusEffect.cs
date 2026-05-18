using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TwinInterceptStatusEffect : StatusEffectBase
    {
        public override string Name => "Twin Intercept";
        public override EffectIconType Icon => EffectIconType.TwinInterceptStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        public TwinInterceptStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
        }

    }
}

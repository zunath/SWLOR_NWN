using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BrutalAssaultStatusEffect : StatusEffectBase
    {
        public override string Name => "Brutal Assault";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;

        public BrutalAssaultStatusEffect()
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 10;
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TauntingDeflectionStatusEffect : StatusEffectBase
    {
        public override string Name => "Taunting Deflection";
        public override EffectIconType Icon => EffectIconType.TauntingDeflectionStatusEffect;
        public TauntingDeflectionStatusEffect()
        {
            StatGroup.Stats[StatType.RangedDeflection] = 10;
            StatGroup.Stats[StatType.DeflectionFPRestore] = 2;
            StatGroup.Stats[StatType.DeflectionEnmityPercentAdjustment] = 20;
        }

    }
}

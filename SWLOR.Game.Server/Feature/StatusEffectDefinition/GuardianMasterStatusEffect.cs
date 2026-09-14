using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardianMasterStatusEffect : StatusEffectBase
    {
        public override string Name => "Guardian Master";
        public override EffectIconType Icon => EffectIconType.GuardianMasterStatusEffect;
        public GuardianMasterStatusEffect()
        {
            StatGroup.Stats[StatType.DeflectionFPRestore] = 4;
            StatGroup.Stats[StatType.DeflectionEnmityPercentAdjustment] = 50;
            StatGroup.Stats[StatType.RangedDeflectionChanceCap] = 10;
        }

    }
}

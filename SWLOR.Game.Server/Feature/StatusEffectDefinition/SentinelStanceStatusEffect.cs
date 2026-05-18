using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SentinelStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Sentinel Stance";
        public override EffectIconType Icon => EffectIconType.SentinelStanceStatusEffect;
        public SentinelStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 15;
            StatGroup.Stats[StatType.AttackDeflection] = 15;
        }

    }
}

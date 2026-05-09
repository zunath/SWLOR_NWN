using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SentinelGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Sentinel Guard";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public SentinelGuardStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 10;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 10;
            StatGroup.Stats[StatType.AttackDeflection] = 10;
        }

    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SentinelGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Sentinel Guard";
        public override EffectIconType Icon => EffectIconType.SentinelGuardStatusEffect;
        public SentinelGuardStatusEffect()
        {
            StatGroup.Stats[StatType.AttackDeflection] = 10;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (creature == Source)
            {
                StatGroup.Stats.Remove(StatType.AttackDeflection);
                StatGroup.Stats[StatType.EnmityPercentAdjustment] = 20;
            }
        }

    }
}

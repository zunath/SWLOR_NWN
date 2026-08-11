using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SentinelGuardStatusEffect : StatusEffectBase
    {
        private readonly int _attackDeflection;
        private readonly int _selfEnmityPercentAdjustment;

        public override string Name => "Sentinel Guard";
        public override EffectIconType Icon => EffectIconType.SentinelGuardStatusEffect;

        public SentinelGuardStatusEffect()
            : this(8, 20)
        {
        }

        public SentinelGuardStatusEffect(int attackDeflection, int selfEnmityPercentAdjustment)
        {
            _attackDeflection = attackDeflection;
            _selfEnmityPercentAdjustment = selfEnmityPercentAdjustment;
            StatGroup.Stats[StatType.MeleeDeflection] = _attackDeflection;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (creature == Source)
            {
                StatGroup.Stats.Remove(StatType.MeleeDeflection);
                StatGroup.Stats[StatType.EnmityPercentAdjustment] = _selfEnmityPercentAdjustment;
            }
        }

        public override IStatusEffect Clone()
        {
            return new SentinelGuardStatusEffect(_attackDeflection, _selfEnmityPercentAdjustment);
        }
    }
}

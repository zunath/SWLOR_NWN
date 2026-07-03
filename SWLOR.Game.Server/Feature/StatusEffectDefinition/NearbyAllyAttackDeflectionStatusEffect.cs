using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class NearbyAllyAttackDeflectionStatusEffect : StatusEffectBase
    {
        private readonly int _attackDeflection;
        private readonly int _selfEnmityPercentAdjustment;

        public override string Name => "Sentinel Guard";
        public override EffectIconType Icon => EffectIconType.SentinelGuardStatusEffect;

        public NearbyAllyAttackDeflectionStatusEffect()
            : this(8, 20)
        {
        }

        public NearbyAllyAttackDeflectionStatusEffect(int attackDeflection, int selfEnmityPercentAdjustment)
        {
            _attackDeflection = attackDeflection;
            _selfEnmityPercentAdjustment = selfEnmityPercentAdjustment;
            StatGroup.Stats[StatType.AttackDeflection] = _attackDeflection;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (creature == Source)
            {
                StatGroup.Stats.Remove(StatType.AttackDeflection);
                StatGroup.Stats[StatType.EnmityPercentAdjustment] = _selfEnmityPercentAdjustment;
            }
        }

        public override IStatusEffect Clone()
        {
            return new NearbyAllyAttackDeflectionStatusEffect(_attackDeflection, _selfEnmityPercentAdjustment);
        }
    }
}

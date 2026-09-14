using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SentinelGuardStatusEffect : StatusEffectBase
    {
        private readonly int _deflection;
        private readonly int _selfEnmityPercentAdjustment;
        private readonly StatType _deflectionStatType;

        public override string Name => "Sentinel Guard";
        public override EffectIconType Icon => EffectIconType.SentinelGuardStatusEffect;

        public SentinelGuardStatusEffect()
            : this(8, 20, Stat.GetGrantedDeflectionStatType(
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection))
        {
        }

        public SentinelGuardStatusEffect(
            int deflection,
            int selfEnmityPercentAdjustment,
            StatType deflectionStatType)
        {
            _deflection = deflection;
            _selfEnmityPercentAdjustment = selfEnmityPercentAdjustment;
            _deflectionStatType = deflectionStatType;
            StatGroup.Stats[_deflectionStatType] = _deflection;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (creature == Source)
            {
                StatGroup.Stats.Remove(_deflectionStatType);
                StatGroup.Stats[StatType.EnmityPercentAdjustment] = _selfEnmityPercentAdjustment;
            }
        }

        public override IStatusEffect Clone()
        {
            return new SentinelGuardStatusEffect(
                _deflection,
                _selfEnmityPercentAdjustment,
                _deflectionStatType);
        }
    }
}

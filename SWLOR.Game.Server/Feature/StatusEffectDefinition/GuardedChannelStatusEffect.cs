using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedChannelStatusEffect : StatusEffectBase
    {
        private readonly int _rangedDeflection;
        private readonly int _forceDefensePercent;

        public override string Name => "Guarded Channel";
        public override EffectIconType Icon => EffectIconType.GuardedChannelStatusEffect;
        public GuardedChannelStatusEffect()
            : this(12, 20)
        {
        }

        public GuardedChannelStatusEffect(int rangedDeflection, int forceDefensePercent)
        {
            _rangedDeflection = rangedDeflection;
            _forceDefensePercent = forceDefensePercent;

            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = forceDefensePercent;
            StatGroup.Stats[StatType.RangedDeflection] = rangedDeflection;
        }

        public override IStatusEffect Clone()
        {
            return new GuardedChannelStatusEffect(_rangedDeflection, _forceDefensePercent);
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedChannelStatusEffect : StatusEffectBase
    {
        private readonly int _attackDeflection;
        private readonly int _forceDefensePercent;

        public override string Name => "Guarded Channel";
        public override EffectIconType Icon => EffectIconType.GuardedChannelStatusEffect;
        public GuardedChannelStatusEffect()
            : this(12, 20)
        {
        }

        public GuardedChannelStatusEffect(int attackDeflection, int forceDefensePercent)
        {
            _attackDeflection = attackDeflection;
            _forceDefensePercent = forceDefensePercent;

            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = forceDefensePercent;
            StatGroup.Stats[StatType.RangedDeflection] = attackDeflection;
        }

        public override IStatusEffect Clone()
        {
            return new GuardedChannelStatusEffect(_attackDeflection, _forceDefensePercent);
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardedChannelStatusEffect : StatusEffectBase
    {
        private readonly int _physicalDefensePercent;

        public override string Name => "Guarded Channel";
        public override EffectIconType Icon => EffectIconType.GuardedChannelStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public GuardedChannelStatusEffect()
            : this(10)
        {
        }

        public GuardedChannelStatusEffect(int physicalDefensePercent)
        {
            _physicalDefensePercent = physicalDefensePercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = physicalDefensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new GuardedChannelStatusEffect(_physicalDefensePercent);
        }
    }
}

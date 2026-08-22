using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UnbreakableStatusEffect : StatusEffectBase
    {
        private readonly int _physicalDefensePercent;

        public override string Name => "Unbreakable";
        public override EffectIconType Icon => EffectIconType.UnbreakableStatusEffect;

        public UnbreakableStatusEffect() : this(40)
        {
        }

        public UnbreakableStatusEffect(int physicalDefensePercent)
        {
            _physicalDefensePercent = physicalDefensePercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = _physicalDefensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new UnbreakableStatusEffect(_physicalDefensePercent);
        }
    }
}

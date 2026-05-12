using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FortressStrikeStatusEffect : StatusEffectBase
    {
        private readonly int _physicalDefensePercent;

        public override string Name => "Fortress Strike";
        public override EffectIconType Icon => EffectIconType.DamageReduction;

        public FortressStrikeStatusEffect()
            : this(10)
        {
        }

        public FortressStrikeStatusEffect(int physicalDefensePercent)
        {
            _physicalDefensePercent = physicalDefensePercent;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = physicalDefensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new FortressStrikeStatusEffect(_physicalDefensePercent);
        }
    }
}

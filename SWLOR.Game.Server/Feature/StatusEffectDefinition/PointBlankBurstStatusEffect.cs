using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PointBlankBurstStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPercent;

        public override string Name => "Point Blank Burst";
        public override EffectIconType Icon => EffectIconType.PointBlankBurstStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public PointBlankBurstStatusEffect()
            : this(5)
        {
        }

        public PointBlankBurstStatusEffect(int evasionPercent)
        {
            _evasionPercent = evasionPercent;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = _evasionPercent;
        }

        public override IStatusEffect Clone()
        {
            return new PointBlankBurstStatusEffect(_evasionPercent);
        }
    }
}

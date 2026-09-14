using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CenteringStatusEffect : StatusEffectBase
    {
        private readonly int _accuracyPercent;

        public override string Name => "Centering";
        public override EffectIconType Icon => EffectIconType.CenteringStatusEffect;

        public CenteringStatusEffect()
            : this(10)
        {
        }

        public CenteringStatusEffect(int accuracyPercent)
        {
            _accuracyPercent = accuracyPercent;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = _accuracyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new CenteringStatusEffect(_accuracyPercent);
        }
    }
}

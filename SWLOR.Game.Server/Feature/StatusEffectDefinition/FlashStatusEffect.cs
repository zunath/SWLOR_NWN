using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlashStatusEffect : StatusEffectBase
    {
        private readonly int _accuracyPenalty;
        public override string Name => "Flash";
        public override EffectIconType Icon => EffectIconType.Blindness;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;

        public FlashStatusEffect()
            : this(2)
        {
        }

        public FlashStatusEffect(int accuracyPenalty)
        {
            _accuracyPenalty = Math.Abs(accuracyPenalty);
            StatGroup.Stats[StatType.Accuracy] = -_accuracyPenalty;
        }

        public override IStatusEffect Clone()
        {
            return new FlashStatusEffect(_accuracyPenalty);
        }
    }
}

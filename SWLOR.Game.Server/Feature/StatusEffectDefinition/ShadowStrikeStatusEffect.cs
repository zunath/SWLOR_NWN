using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShadowStrikeStatusEffect : MovementSpeedStatusEffectBase
    {
        private readonly int _movementSpeedPercentAdjustment;

        public override string Name => "Shadow Strike";
        public override EffectIconType Icon => EffectIconType.ShadowStrikeStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        protected override int MovementSpeedPercentAdjustment => _movementSpeedPercentAdjustment;

        public ShadowStrikeStatusEffect()
            : this(-30)
        {
        }

        public ShadowStrikeStatusEffect(int movementSpeedPercentAdjustment)
        {
            _movementSpeedPercentAdjustment = movementSpeedPercentAdjustment;
        }

        public override IStatusEffect Clone()
        {
            return new ShadowStrikeStatusEffect(_movementSpeedPercentAdjustment);
        }
    }
}

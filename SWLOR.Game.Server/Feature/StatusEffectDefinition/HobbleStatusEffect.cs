using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HobbleStatusEffect : MovementSpeedStatusEffectBase
    {
        public override string Name => "Hobble";
        public override EffectIconType Icon => EffectIconType.HobbleStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        protected override int MovementSpeedPercentAdjustment => -50;

        // Area pulses re-apply Hobble every few seconds; each application sets the same fixed slow.
        public override bool CanRefreshInPlace(IStatusEffect active) => active is HobbleStatusEffect;
    }
}

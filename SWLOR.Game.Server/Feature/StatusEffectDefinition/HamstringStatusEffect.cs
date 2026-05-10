using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HamstringStatusEffect : MovementSpeedStatusEffectBase
    {
        public override string Name => "Hamstring";
        public override EffectIconType Icon => EffectIconType.MovementSpeedDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        protected override int MovementSpeedPercentAdjustment => -20;
    }
}

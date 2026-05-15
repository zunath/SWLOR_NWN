using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CoveringStrikeStatusEffect : StatusEffectBase
    {
        public override string Name => "Covering Strike";
        public override EffectIconType Icon => EffectIconType.Taunted;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public CoveringStrikeStatusEffect()
        {
            StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = 25;
        }
    }
}

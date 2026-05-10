using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ExhaustedStatusEffect : StatusEffectBase
    {
        public override string Name => "Exhausted";
        public override EffectIconType Icon => EffectIconType.Fatigue;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public ExhaustedStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -10;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -10;
        }

    }
}

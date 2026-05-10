using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SunderStatusEffect : StatusEffectBase
    {
        public override string Name => "Sunder";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public SunderStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -15;
        }

    }
}

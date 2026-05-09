using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CripplingDefenseStatusEffect : StatusEffectBase
    {
        public override string Name => "Crippling Defense";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public CripplingDefenseStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -35;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -35;
        }

    }
}

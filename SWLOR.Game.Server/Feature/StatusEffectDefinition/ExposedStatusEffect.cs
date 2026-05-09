using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ExposedStatusEffect : StatusEffectBase
    {
        public override string Name => "Exposed";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public ExposedStatusEffect()
        {
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -15;
        }

    }
}

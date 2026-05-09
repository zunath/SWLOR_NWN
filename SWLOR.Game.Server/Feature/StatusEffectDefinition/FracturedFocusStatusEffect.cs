using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FracturedFocusStatusEffect : StatusEffectBase
    {
        public override string Name => "Fractured Focus";
        public override EffectIconType Icon => EffectIconType.SpellResistanceDecrease;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        public FracturedFocusStatusEffect()
        {
            StatGroup.Stats[StatType.FPCostPercentAdjustment] = 100;
        }
    }
}

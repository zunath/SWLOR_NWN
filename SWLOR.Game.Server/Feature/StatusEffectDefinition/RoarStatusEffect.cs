using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RoarStatusEffect : StatusEffectBase
    {
        public override string Name => "Roar";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        public RoarStatusEffect()
        {
            StatGroup.Stats[StatType.Defense] = -3;
        }
    }
}

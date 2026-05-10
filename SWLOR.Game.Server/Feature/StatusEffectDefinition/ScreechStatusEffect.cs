using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ScreechStatusEffect : StatusEffectBase
    {
        public override string Name => "Screech";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        public ScreechStatusEffect()
        {
            StatGroup.Stats[StatType.Defense] = -10;
        }
    }
}

using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class DisturbanceStatusEffectBase : StatusEffectBase
    {
        protected DisturbanceStatusEffectBase(int accuracyPenalty)
        {
            StatGroup.Stats[StatType.Accuracy] = -accuracyPenalty;
        }

        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
    }
}

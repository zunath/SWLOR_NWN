using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeakenedStatusEffect : StatusEffectBase
    {
        public override string Name => "Weakened";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public WeakenedStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
        }

    }
}

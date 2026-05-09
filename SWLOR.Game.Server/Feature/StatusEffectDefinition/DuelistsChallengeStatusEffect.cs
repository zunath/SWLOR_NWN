using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistsChallengeStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist's Challenge";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;

        public DuelistsChallengeStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 20;
        }
    }
}

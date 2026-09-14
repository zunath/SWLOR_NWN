using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistsChallengeSelfStatusEffect : StatusEffectBase
    {
        public override string Name => "Duelist's Challenge";
        public override EffectIconType Icon => EffectIconType.DuelistsChallengeSelfStatusEffect;

        public DuelistsChallengeSelfStatusEffect()
        {
            StatGroup.Stats[StatType.DefenseAgainstStatusSourcePercentAdjustment] = 20;
        }
    }
}

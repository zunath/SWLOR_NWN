using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadMansHandStatusEffect : StatusEffectBase
    {
        public override string Name => "Dead Man's Hand";
        public override EffectIconType Icon => EffectIconType.DeadMansHandStatusEffect;

        public DeadMansHandStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment] = 10;
        }
    }
}

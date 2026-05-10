using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadeyeStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Deadeye Stance";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;
        public DeadeyeStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 15;
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }

    }
}

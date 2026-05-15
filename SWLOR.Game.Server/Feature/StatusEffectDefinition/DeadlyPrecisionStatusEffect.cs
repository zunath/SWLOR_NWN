using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DeadlyPrecisionStatusEffect : StatusEffectBase
    {
        public override string Name => "Deadly Precision";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public DeadlyPrecisionStatusEffect()
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 15;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }

    }
}

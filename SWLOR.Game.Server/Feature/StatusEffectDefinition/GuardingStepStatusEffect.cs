using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingStepStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding Step";
        public override EffectIconType Icon => EffectIconType.GuardingStepStatusEffect;
        public GuardingStepStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 25;
        }

    }
}

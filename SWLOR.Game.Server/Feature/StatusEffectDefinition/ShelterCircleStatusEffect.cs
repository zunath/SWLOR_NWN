using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShelterCircleStatusEffect : StatusEffectBase
    {
        public override string Name => "Shelter Circle";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public ShelterCircleStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = 20;
        }

    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShieldWallStatusEffect : StatusEffectBase
    {
        public override string Name => "Shield Wall";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public ShieldWallStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
        }

    }
}

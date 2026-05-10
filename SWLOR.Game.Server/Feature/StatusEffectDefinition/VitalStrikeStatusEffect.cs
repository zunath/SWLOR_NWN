using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class VitalStrikeStatusEffect : StatusEffectBase
    {
        public override string Name => "Vital Strike";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public VitalStrikeStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -10;
        }

    }
}

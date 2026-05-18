using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FerocityStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Ferocity Stance";
        public override EffectIconType Icon => EffectIconType.FerocityStanceStatusEffect;
        public FerocityStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 10;
            StatGroup.Stats[StatType.OffhandAttackDelayReductionPercent] = 20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }

    }
}

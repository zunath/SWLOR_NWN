using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BerserkerStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Berserker Stance";
        public override EffectIconType Icon => EffectIconType.Haste;
        public BerserkerStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 10;
        }

    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CycloneStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Cyclone Stance";
        public override EffectIconType Icon => EffectIconType.CycloneStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public CycloneStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 10;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
        }

    }
}

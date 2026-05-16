using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TempestStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Tempest Stance";
        public override EffectIconType Icon => EffectIconType.Haste;
        public TempestStanceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = 10;
        }

    }
}

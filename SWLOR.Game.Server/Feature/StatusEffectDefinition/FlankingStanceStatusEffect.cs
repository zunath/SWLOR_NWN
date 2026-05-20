using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FlankingStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Flanking Stance";
        public override EffectIconType Icon => EffectIconType.FlankingStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public FlankingStanceStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -25;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -25;
            StatGroup.Stats[StatType.SideAttackDamagePercentAdjustment] = 20;
            StatGroup.Stats[StatType.SideAttackHitChancePercentAdjustment] = 15;
        }

    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Apex Collapse stance: while active, trades defense for raw offense. A Mimicry offensive stance,
    /// modelled on Berserker Stance.
    /// </summary>
    public sealed class ApexCollapseStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Apex Collapse Stance";
        public override EffectIconType Icon => EffectIconType.ApexCollapseStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new ApexCollapseStanceStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 25;
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
        }
    }
}

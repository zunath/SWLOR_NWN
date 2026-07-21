using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Apex Collapse stance: while active, trades defense for raw offense. A Mimicry offensive stance,
    /// modelled on Berserker Stance.
    /// </summary>
    public sealed class ApexCollapseStatusEffect : StatusEffectBase
    {
        public override string Name => "Apex Collapse";
        public override EffectIconType Icon => EffectIconType.ApexCollapseStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new ApexCollapseStatusEffect();
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

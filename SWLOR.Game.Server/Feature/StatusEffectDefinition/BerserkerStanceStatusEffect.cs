using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BerserkerStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Berserker Stance";
        public override EffectIconType Icon => EffectIconType.BerserkerStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new BerserkerStanceStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 25;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DefensiveStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Defensive Stance";
        public override EffectIconType Icon => EffectIconType.DefensiveStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new DefensiveStanceStatusEffect();
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 30;
        }
    }
}

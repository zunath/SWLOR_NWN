using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BerserkerStanceStatusEffect : StatusEffectBase
    {
        private readonly int _level;

        public BerserkerStanceStatusEffect() : this(1)
        {
        }

        public BerserkerStanceStatusEffect(int level)
        {
            _level = Math.Max(1, level);
        }

        public override string Name => "Berserker Stance";
        public override EffectIconType Icon => EffectIconType.BerserkerStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new BerserkerStanceStatusEffect(_level);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = _level >= 2 ? 25 : 15;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = _level >= 2 ? 15 : 10;
        }
    }
}

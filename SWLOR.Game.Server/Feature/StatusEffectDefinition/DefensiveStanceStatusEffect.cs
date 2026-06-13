using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DefensiveStanceStatusEffect : StatusEffectBase
    {
        private readonly int _level;

        public DefensiveStanceStatusEffect() : this(1)
        {
        }

        public DefensiveStanceStatusEffect(int level)
        {
            _level = Math.Max(1, level);
        }

        public override string Name => "Defensive Stance";
        public override EffectIconType Icon => EffectIconType.DefensiveStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public override IStatusEffect Clone()
        {
            return new DefensiveStanceStatusEffect(_level);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            var defense = _level >= 2 ? 20 : 15;

            StatGroup.Stats[StatType.AttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -20;
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = defense;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = defense;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = _level >= 2 ? 30 : 20;
        }
    }
}

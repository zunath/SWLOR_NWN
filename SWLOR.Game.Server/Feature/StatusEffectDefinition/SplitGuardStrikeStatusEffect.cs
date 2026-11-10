using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SplitGuardStrikeStatusEffect : StatusEffectBase
    {
        private readonly int _defensePercent;

        public override string Name => "Split Guard Strike";
        public override EffectIconType Icon => EffectIconType.SplitGuardStrikeStatusEffect;

        public SplitGuardStrikeStatusEffect()
            : this(15)
        {
        }

        public SplitGuardStrikeStatusEffect(int defensePercent)
        {
            _defensePercent = defensePercent;
            StatGroup.Stats[StatType.DefensePercentAdjustment] = defensePercent;
        }

        public override IStatusEffect Clone()
        {
            return new SplitGuardStrikeStatusEffect(_defensePercent);
        }
    }
}

using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SpotterStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Spotter Stance";
        public override EffectIconType Icon => EffectIconType.SpotterStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
        public SpotterStanceStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 15;
            StatGroup.Stats[StatType.RangedEvasionPercentAdjustment] = 15;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = -10;
        }

    }
}

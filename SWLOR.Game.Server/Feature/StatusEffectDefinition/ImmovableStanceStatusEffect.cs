using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImmovableStanceStatusEffect : StatusEffectBase
    {
        public override string Name => "Immovable Stance";
        public override EffectIconType Icon => EffectIconType.ImmovableStanceStatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;

        public ImmovableStanceStatusEffect()
        {
            StatGroup.Stats[StatType.DamageEnmityPercentAdjustment] = 100;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 30;
            StatGroup.Stats[StatType.MobilityResistance] = 20;
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = -15;
        }
    }
}

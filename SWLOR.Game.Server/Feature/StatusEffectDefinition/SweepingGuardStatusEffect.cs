using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SweepingGuardStatusEffect : StatusEffectBase
    {
        public override string Name => "Sweeping Guard";
        public override EffectIconType Icon => EffectIconType.SweepingGuardStatusEffect;

        public SweepingGuardStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
        }
    }
}

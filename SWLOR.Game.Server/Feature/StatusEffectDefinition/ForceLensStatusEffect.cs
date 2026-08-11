using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceLensStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Lens";
        public override EffectIconType Icon => EffectIconType.ForceLensStatusEffect;
        public ForceLensStatusEffect()
        {
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 15;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (Source != creature)
                return;

            StatGroup.Stats.Remove(StatType.ForceDefensePercentAdjustment);
            StatGroup.Stats[StatType.RangedDeflection] = 8;
        }
    }
}

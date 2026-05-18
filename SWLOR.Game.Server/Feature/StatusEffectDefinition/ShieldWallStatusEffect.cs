using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShieldWallStatusEffect : StatusEffectBase
    {
        public override string Name => "Shield Wall";
        public override EffectIconType Icon => EffectIconType.ShieldWallStatusEffect;
        public ShieldWallStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 15;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (Source != creature)
                return;

            StatGroup.Stats.Remove(StatType.PhysicalDefensePercentAdjustment);
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 25;
        }
    }
}

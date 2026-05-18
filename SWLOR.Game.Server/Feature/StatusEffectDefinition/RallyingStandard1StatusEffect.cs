using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RallyingStandard1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Rallying Standard I";
        public override EffectIconType Icon => EffectIconType.RallyingStandard1StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = ScaleBySourceSocial(3, 4);
        }
    }
}

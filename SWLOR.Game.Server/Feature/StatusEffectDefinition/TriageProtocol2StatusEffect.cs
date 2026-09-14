using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TriageProtocol2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Triage Protocol II";
        public override EffectIconType Icon => EffectIconType.TriageProtocol2StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.HealingReceivedPercentAdjustment] = ScaleBySourceSocial(12, 15);
        }
    }
}

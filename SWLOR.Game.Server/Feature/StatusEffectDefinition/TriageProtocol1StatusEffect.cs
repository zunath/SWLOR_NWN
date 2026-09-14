using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TriageProtocol1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Triage Protocol I";
        public override EffectIconType Icon => EffectIconType.TriageProtocol1StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.HealingReceivedPercentAdjustment] = ScaleBySourceSocial(8, 10);
        }
    }
}

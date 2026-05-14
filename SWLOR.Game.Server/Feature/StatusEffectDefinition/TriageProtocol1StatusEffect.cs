using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TriageProtocol1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Triage Protocol I";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override bool PersistsOnLogout => false;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(TriageProtocol2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.HealingReceivedPercentAdjustment] = ScaleBySourceSocial(5, 7);
        }
    }
}

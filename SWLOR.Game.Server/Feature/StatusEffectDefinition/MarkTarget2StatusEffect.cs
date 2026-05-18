using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkTarget2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Mark Target II";
        public override EffectIconType Icon => EffectIconType.MarkTarget2StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageTakenFromStatusSourcePartyPercentAdjustment] = ScaleBySourceSocial(12, 15);
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -ScaleBySourceSocial(10, 12);
        }
    }
}

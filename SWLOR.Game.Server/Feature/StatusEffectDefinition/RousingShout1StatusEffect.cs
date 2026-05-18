using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RousingShout1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Rousing Shout I";
        public override EffectIconType Icon => EffectIconType.RousingShout1StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -ScaleBySourceSocial(10, 12);
        }
    }
}

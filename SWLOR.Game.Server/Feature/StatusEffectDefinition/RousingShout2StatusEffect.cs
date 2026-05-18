using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RousingShout2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Rousing Shout II";
        public override EffectIconType Icon => EffectIconType.RousingShout2StatusEffect;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(RousingShout1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -ScaleBySourceSocial(15, 18);
        }
    }
}

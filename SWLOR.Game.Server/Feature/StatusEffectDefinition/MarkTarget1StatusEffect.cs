using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkTarget1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Mark Target I";
        public override EffectIconType Icon => EffectIconType.MarkTarget1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(8, 10);
        }
    }
}

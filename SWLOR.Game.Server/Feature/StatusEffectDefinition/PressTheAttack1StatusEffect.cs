using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressTheAttack1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Press the Attack I";
        public override EffectIconType Icon => EffectIconType.PressTheAttack1StatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(6, 8);
        }
    }
}

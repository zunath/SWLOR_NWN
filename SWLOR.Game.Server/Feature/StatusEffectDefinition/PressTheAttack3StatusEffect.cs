using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PressTheAttack3StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Press the Attack III";
        public override EffectIconType Icon => EffectIconType.PressTheAttack3StatusEffect;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = ScaleBySourceSocial(10, 12);
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = ScaleBySourceSocial(5, 7);
        }
    }
}

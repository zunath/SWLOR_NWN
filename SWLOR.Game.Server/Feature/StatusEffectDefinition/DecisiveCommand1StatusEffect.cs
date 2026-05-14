using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DecisiveCommand1StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Decisive Command";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = ScaleBySourceSocial(14, 18);
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = ScaleBySourceSocial(8, 10);
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = ScaleBySourceSocial(8, 10);
        }
    }
}

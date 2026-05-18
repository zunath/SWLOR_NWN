using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CoordinatedFocus2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Coordinated Focus II";
        public override EffectIconType Icon => EffectIconType.CoordinatedFocus2StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = ScaleBySourceSocial(4, 5);
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = ScaleBySourceSocial(5, 7);
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SteadyFormation1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Steady Formation I";
        public override EffectIconType Icon => EffectIconType.SteadyFormation1StatusEffect;

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = ScaleBySourceSocial(3, 4);
            StatGroup.Stats[StatType.MindResistance] = ScaleBySourceSocial(30, 40);
            StatGroup.Stats[StatType.MobilityResistance] = ScaleBySourceSocial(30, 40);
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SteadyFormation1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Steady Formation I";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(SteadyFormation2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = ScaleBySourceSocial(2, 3);
            StatGroup.Stats[StatType.MindResistance] = ScaleBySourceSocial(20, 25);
            StatGroup.Stats[StatType.MobilityResistance] = ScaleBySourceSocial(20, 25);
        }
    }
}

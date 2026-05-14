using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SteadyFormation2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Steady Formation II";
        public override EffectIconType Icon => EffectIconType.ACIncrease;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(SteadyFormation1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = ScaleBySourceSocial(3, 4);
            StatGroup.Stats[StatType.MindResistance] = ScaleBySourceSocial(35, 45);
            StatGroup.Stats[StatType.MobilityResistance] = ScaleBySourceSocial(35, 45);
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ChargeOrder1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Charge Order I";
        public override EffectIconType Icon => EffectIconType.Haste;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(ChargeOrder2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = ScaleBySourceSocial(10, 12);
            StatGroup.Stats[StatType.MobilityResistance] = ScaleBySourceSocial(30, 40);
        }
    }
}

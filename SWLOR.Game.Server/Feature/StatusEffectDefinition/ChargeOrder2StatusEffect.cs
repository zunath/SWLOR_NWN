using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ChargeOrder2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Charge Order II";
        public override EffectIconType Icon => EffectIconType.Haste;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(ChargeOrder1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = ScaleBySourceSocial(15, 18);
            StatGroup.Stats[StatType.MobilityResistance] = ScaleBySourceSocial(50, 65);
        }
    }
}

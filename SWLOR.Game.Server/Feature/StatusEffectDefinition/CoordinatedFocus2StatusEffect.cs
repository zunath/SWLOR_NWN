using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CoordinatedFocus2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Coordinated Focus II";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(CoordinatedFocus3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(CoordinatedFocus1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            var bonus = ScaleBySourceSocial(3, 4);
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = bonus;
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = bonus;
        }
    }
}

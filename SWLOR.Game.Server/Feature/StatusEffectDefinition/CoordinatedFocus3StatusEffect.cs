using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CoordinatedFocus3StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Coordinated Focus III";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(CoordinatedFocus1StatusEffect),
            typeof(CoordinatedFocus2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = ScaleBySourceSocial(6, 7);
            StatGroup.Stats[StatType.CriticalDamagePercentAdjustment] = ScaleBySourceSocial(8, 10);
        }
    }
}

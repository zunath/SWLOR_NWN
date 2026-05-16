using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CoordinatedFocus1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Coordinated Focus I";
        public override EffectIconType Icon => EffectIconType.DamageIncrease;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(CoordinatedFocus2StatusEffect),
            typeof(CoordinatedFocus3StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.CriticalRatePercentAdjustment] = ScaleBySourceSocial(3, 4);
        }
    }
}

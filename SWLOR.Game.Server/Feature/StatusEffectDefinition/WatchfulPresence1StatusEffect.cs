using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WatchfulPresence1StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Watchful Presence I";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(WatchfulPresence2StatusEffect),
            typeof(WatchfulPresence3StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(4, 5);
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = reduction;
        }
    }
}

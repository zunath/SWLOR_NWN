using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WatchfulPresence2StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Watchful Presence II";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override List<Type> MorePowerfulEffectTypes { get; } = new()
        {
            typeof(WatchfulPresence3StatusEffect),
        };
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(WatchfulPresence1StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(6, 7);
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = reduction;
        }
    }
}

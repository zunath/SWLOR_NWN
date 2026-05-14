using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WatchfulPresence3StatusEffect : AuraStatusEffectBase
    {
        public override string Name => "Watchful Presence III";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(WatchfulPresence1StatusEffect),
            typeof(WatchfulPresence2StatusEffect),
        };

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(5, 6);
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = reduction;
        }
    }
}

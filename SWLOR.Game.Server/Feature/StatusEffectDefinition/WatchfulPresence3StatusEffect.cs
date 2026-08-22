using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WatchfulPresence3StatusEffect : AuraStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Watchful Presence III";
        public override EffectIconType Icon => EffectIconType.WatchfulPresence3StatusEffect;
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(8, 10);
            StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction;
            LeadershipDamageReductionStats = new Dictionary<StatType, int>
            {
                [StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction,
                [StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction,
            };
        }
    }
}

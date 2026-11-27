using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WatchfulPresence2StatusEffect : AuraStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Watchful Presence II";
        public override EffectIconType Icon => EffectIconType.WatchfulPresence2StatusEffect;
        public override List<Type> LessPowerfulEffectTypes { get; } = new()
        {
            typeof(WatchfulPresence1StatusEffect),
        };
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();
        public override (int Base, int Maximum) StrengthScale => (6, 7);

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(StrengthScale.Base, StrengthScale.Maximum);
            StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction;

            // A ship takes Leadership's "other" damage and reads its own reduction stat.
            StatGroup.Stats[StatType.SpaceLeadershipDamageTakenPercentAdjustment] = reduction;
            LeadershipDamageReductionStats = new Dictionary<StatType, int>
            {
                [StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction,
                [StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction,
                [StatType.SpaceLeadershipDamageTakenPercentAdjustment] = reduction,
            };
        }
    }
}

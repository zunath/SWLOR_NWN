using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HoldTheLine1StatusEffect : SocialScalingStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Hold the Line";
        public override EffectIconType Icon => EffectIconType.HoldTheLine1StatusEffect;
        public override bool PersistsOnLogout => false;
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(18, 22);
            StatGroup.Stats[StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.LeadershipOtherDamageTakenPercentAdjustment] = reduction;
            StatGroup.Resists[ResistanceType.Mind] = Resistance.MaximumResistance;
            StatGroup.Resists[ResistanceType.Mobility] = Resistance.MaximumResistance;
            LeadershipDamageReductionStats = new Dictionary<StatType, int>
            {
                [StatType.LeadershipPhysicalDamageTakenPercentAdjustment] = reduction,
                [StatType.LeadershipForceDamageTakenPercentAdjustment] = reduction,
                [StatType.LeadershipOtherDamageTakenPercentAdjustment] = reduction,
            };
        }
    }
}

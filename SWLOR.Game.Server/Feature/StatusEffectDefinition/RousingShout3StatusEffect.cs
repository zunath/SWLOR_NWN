using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RousingShout3StatusEffect : SocialScalingStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Rousing Shout III";
        public override EffectIconType Icon => EffectIconType.RousingShout3StatusEffect;
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(20, 25);
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

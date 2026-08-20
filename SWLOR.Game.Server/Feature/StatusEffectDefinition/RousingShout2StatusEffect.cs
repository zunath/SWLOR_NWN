using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RousingShout2StatusEffect : SocialScalingStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Rousing Shout II";
        public override EffectIconType Icon => EffectIconType.RousingShout2StatusEffect;
        public override List<Type> LessPowerfulEffectTypes { get; } = new List<Type>
        {
            typeof(RousingShout1StatusEffect),
        };
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(15, 18);
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = reduction;
            LeadershipDamageReductionStats = new Dictionary<StatType, int>
            {
                [StatType.PhysicalDamageTakenPercentAdjustment] = reduction,
                [StatType.ForceDamageTakenPercentAdjustment] = reduction,
            };
        }
    }
}

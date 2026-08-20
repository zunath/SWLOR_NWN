using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterResolve2StatusEffect : SocialScalingStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Bolster Resolve II";
        public override EffectIconType Icon => EffectIconType.BolsterResolve2StatusEffect;
        public override bool PersistsOnLogout => false;
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(12, 15);
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

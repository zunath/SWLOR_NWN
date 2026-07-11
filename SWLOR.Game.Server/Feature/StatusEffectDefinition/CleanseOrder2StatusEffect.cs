using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CleanseOrder2StatusEffect : SocialScalingStatusEffectBase, ILeadershipDamageReductionStatusEffect
    {
        public override string Name => "Cleanse Order II";
        public override EffectIconType Icon => EffectIconType.CleanseOrder2StatusEffect;
        public override bool PersistsOnLogout => false;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;
        public IReadOnlyDictionary<StatType, int> LeadershipDamageReductionStats { get; private set; } = new Dictionary<StatType, int>();

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(12, 15);
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = reduction;
            LeadershipDamageReductionStats = new Dictionary<StatType, int>
            {
                [StatType.DamageTakenPercentAdjustment] = reduction,
            };
        }
    }
}

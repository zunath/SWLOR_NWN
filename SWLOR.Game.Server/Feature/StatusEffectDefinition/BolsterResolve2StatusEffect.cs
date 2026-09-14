using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BolsterResolve2StatusEffect : SocialScalingStatusEffectBase
    {
        public override string Name => "Bolster Resolve II";
        public override EffectIconType Icon => EffectIconType.BolsterResolve2StatusEffect;
        public override bool PersistsOnLogout => false;

        protected override void Apply(uint creature, int durationTicks)
        {
            var reduction = -ScaleBySourceSocial(12, 15);
            StatGroup.Stats[StatType.LeadershipRecoveryPhysicalDamageTakenPercentAdjustment] = reduction;
            StatGroup.Stats[StatType.LeadershipRecoveryForceDamageTakenPercentAdjustment] = reduction;
        }
    }
}

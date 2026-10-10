using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PredatoryBondStanceBeastStatusEffect : StatusEffectBase
    {
        public override string Name => "Predatory Bond Stance";
        public override EffectIconType Icon => EffectIconType.PredatoryBondStanceBeastStatusEffect;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public PredatoryBondStanceBeastStatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 25;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 15;
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = 10;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = -40;
        }
    }
}

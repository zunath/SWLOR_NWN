using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CruelMomentumStatusEffect : StatusEffectBase
    {
        public override string Name => "Cruel Momentum";
        public override EffectIconType Icon => EffectIconType.CruelMomentumStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public CruelMomentumStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Force;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 5;
        }
    }
}

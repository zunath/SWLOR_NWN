using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StormcoreMatrixStatusEffect : StatusEffectBase
    {
        public override string Name => "Stormcore Matrix";
        public override EffectIconType Icon => EffectIconType.StormcoreMatrixStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public StormcoreMatrixStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 8;
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment] = 8;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -8;
        }
    }
}

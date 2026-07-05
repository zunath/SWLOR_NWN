using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FluxDiverterStatusEffect : StatusEffectBase
    {
        public override string Name => "Flux Diverter";
        public override EffectIconType Icon => EffectIconType.FluxDiverterStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public FluxDiverterStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = 5;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType] = (int)SkillType.Devices;
            StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment] = 5;
        }
    }
}

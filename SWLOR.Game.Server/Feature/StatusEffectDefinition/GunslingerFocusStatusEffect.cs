using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GunslingerFocusStatusEffect : StatusEffectBase
    {
        public const int CriticalRateBonus = 10;
        public const int StaminaCostReduction = 2;

        public override string Name => "Gunslinger Focus";
        public override EffectIconType Icon => EffectIconType.GunslingerFocusStatusEffect;

        public GunslingerFocusStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustmentSkillType] = (int)SkillType.Pistol;
            StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment] = CriticalRateBonus;
            StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType] = (int)SkillType.Pistol;
            StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustment] = -StaminaCostReduction;
        }
    }
}

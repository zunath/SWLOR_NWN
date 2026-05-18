using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GunslingerFocusStatusEffect : StatusEffectBase
    {
        public const int DamageBonus = 10;
        public const int StaminaCostReduction = 2;

        public override string Name => "Gunslinger Focus";
        public override EffectIconType Icon => EffectIconType.GunslingerFocusStatusEffect;

        public GunslingerFocusStatusEffect()
        {
            StatGroup.Stats[StatType.AbilityDamageFlatAdjustmentPerkType] = (int)PerkType.QuickDraw;
            StatGroup.Stats[StatType.AbilityDamageFlatAdjustmentSecondaryPerkType] = (int)PerkType.DoubleShot;
            StatGroup.Stats[StatType.AbilityDamageFlatAdjustment] = DamageBonus;
            StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustmentPerkType] = (int)PerkType.QuickDraw;
            StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType] = (int)PerkType.DoubleShot;
            StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustment] = -StaminaCostReduction;
        }
    }
}

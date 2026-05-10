using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceErosionStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Erosion";
        public override EffectIconType Icon => EffectIconType.DamageImmunityMagicDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public ForceErosionStatusEffect()
        {
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -10;
        }

    }
}

using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class VitalStrikeStatusEffect : StatusEffectBase
    {
        public override string Name => "Vital Strike";
        public override EffectIconType Icon => EffectIconType.VitalStrikeStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public VitalStrikeStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -10;
        }

    }
}

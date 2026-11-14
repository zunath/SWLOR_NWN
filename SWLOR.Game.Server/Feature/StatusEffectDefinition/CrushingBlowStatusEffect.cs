using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CrushingBlowStatusEffect : StatusEffectBase
    {
        public override string Name => "Crushing Blow";
        public override EffectIconType Icon => EffectIconType.CrushingBlowStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public CrushingBlowStatusEffect()
        {
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -15;
        }
    }
}

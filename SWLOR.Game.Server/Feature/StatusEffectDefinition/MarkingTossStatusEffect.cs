using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MarkingTossStatusEffect : StatusEffectBase
    {
        public override string Name => "Marking Toss";
        public override EffectIconType Icon => EffectIconType.MarkingTossStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public MarkingTossStatusEffect()
        {
            StatGroup.Stats[StatType.ThrowingDamageTakenPercentAdjustment] = 10;
        }
    }
}

using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TempestMarkStatusEffect : StatusEffectBase
    {
        public override string Name => "Tempest Mark";
        public override EffectIconType Icon => EffectIconType.TempestMarkStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override StatusEffectStackType StackingType => StatusEffectStackType.UnlimitedStacking;

        public TempestMarkStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = 2;
        }
    }
}

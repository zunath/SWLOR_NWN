using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HemorrhageStatusEffect : StatusEffectBase
    {
        public override string Name => "Hemorrhage";
        public override EffectIconType Icon => EffectIconType.HemorrhageStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        // Machines cannot bleed, independent of their Trauma resistance.
        public override string CanApply(uint creature)
        {
            return Droid.IsBloodlessMachine(creature) ? "Droids cannot bleed." : string.Empty;
        }

        public HemorrhageStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 10;
        }
    }
}

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

        public HemorrhageStatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = 10;
        }
    }
}

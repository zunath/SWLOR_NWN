using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PredatorsMark1StatusEffect : StatusEffectBase
    {
        public override string Name => "Predator's Mark I";
        public override EffectIconType Icon => EffectIconType.PredatorsMark1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;

        public PredatorsMark1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment] = 10;
        }
    }
}

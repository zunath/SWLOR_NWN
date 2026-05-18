using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IncapacitateStatusEffect : StatusEffectBase
    {
        public override string Name => "Incapacitate";
        public override EffectIconType Icon => EffectIconType.IncapacitateStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public IncapacitateStatusEffect()
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }

    }
}

using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BreachStatusEffect : StatusEffectBase
    {
        public override string Name => "Breach";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public BreachStatusEffect()
        {
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -20;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -20;
        }
    }
}

using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SmokeBombStatusEffect : StatusEffectBase
    {
        public override string Name => "Smoke Bomb";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public SmokeBombStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -20;
        }
    }
}

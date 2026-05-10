using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EssenceDrainStatusEffect : StatusEffectBase
    {
        public override string Name => "Essence Drain";
        public override EffectIconType Icon => EffectIconType.LevelDrain;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public EssenceDrainStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -15;
        }

    }
}

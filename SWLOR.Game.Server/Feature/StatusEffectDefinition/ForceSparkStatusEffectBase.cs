using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ForceSparkStatusEffectBase : StatusEffectBase
    {
        protected ForceSparkStatusEffectBase(int evasionPenalty)
        {
            StatGroup.Stats[StatType.Evasion] = -evasionPenalty;
        }

        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
    }
}

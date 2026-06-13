using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceJudgment3StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Judgment III";
        public override EffectIconType Icon => EffectIconType.ForceJudgment3StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public ForceJudgment3StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = -8;
        }
    }
}

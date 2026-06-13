using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceJudgment2StatusEffect : StatusEffectBase
    {
        public override string Name => "Force Judgment II";
        public override EffectIconType Icon => EffectIconType.ForceJudgment2StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public ForceJudgment2StatusEffect()
        {
            StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment] = -6;
        }
    }
}

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DisorientedStatusEffect : StatusEffectBase
    {
        public override string Name => "Disoriented";
        public override EffectIconType Icon => EffectIconType.Confused;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;
        public DisorientedStatusEffect()
        {
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -15;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] += Stat.GetStatAdjustment(
                Source,
                StatType.OutgoingDisorientedAttackPercentAdjustment);
            StatGroup.Stats[StatType.EvasionPercentAdjustment] += Stat.GetStatAdjustment(
                Source,
                StatType.OutgoingDisorientedEvasionPercentAdjustment);
        }

    }
}

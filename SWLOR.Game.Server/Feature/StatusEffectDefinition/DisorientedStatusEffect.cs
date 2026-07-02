using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DisorientedStatusEffect : StatusEffectBase
    {
        private readonly int _accuracyPenaltyPercent;
        private readonly int _evasionPenaltyPercent;

        public override string Name => "Disoriented";
        public override EffectIconType Icon => EffectIconType.DisorientedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;
        public DisorientedStatusEffect()
            : this(0)
        {
        }

        public DisorientedStatusEffect(int additionalEvasionPenaltyPercent)
            : this(15, 15 + Math.Max(0, additionalEvasionPenaltyPercent))
        {
        }

        public DisorientedStatusEffect(int accuracyPenaltyPercent, int evasionPenaltyPercent)
        {
            _accuracyPenaltyPercent = Math.Abs(accuracyPenaltyPercent);
            _evasionPenaltyPercent = Math.Abs(evasionPenaltyPercent);
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -_accuracyPenaltyPercent;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -_evasionPenaltyPercent;
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

        public override IStatusEffect Clone()
        {
            return new DisorientedStatusEffect(_accuracyPenaltyPercent, _evasionPenaltyPercent);
        }
    }
}

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DisorientedStatusEffect : StatusEffectBase
    {
        private readonly int _additionalEvasionPenaltyPercent;

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
        {
            _additionalEvasionPenaltyPercent = Math.Max(0, additionalEvasionPenaltyPercent);
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -15;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -15 - _additionalEvasionPenaltyPercent;
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
            return new DisorientedStatusEffect(_additionalEvasionPenaltyPercent);
        }
    }
}

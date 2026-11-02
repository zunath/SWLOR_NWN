using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Lowers the bearer's Accuracy against the creature that applied it. Each caster's Decoy is
    /// tracked separately because the penalty only applies against that caster.
    /// </summary>
    public sealed class DecoyStatusEffect : StatusEffectBase
    {
        public const int DefaultAccuracyPenaltyPercent = 25;

        private readonly int _accuracyPenaltyPercent;

        public override string Name => "Decoy";
        public override EffectIconType Icon => EffectIconType.DecoyStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;
        public override StatusEffectStackType StackingType => StatusEffectStackType.StackFromMultipleSources;

        public DecoyStatusEffect()
            : this(DefaultAccuracyPenaltyPercent)
        {
        }

        public DecoyStatusEffect(int accuracyPenaltyPercent)
        {
            _accuracyPenaltyPercent = Math.Abs(accuracyPenaltyPercent);
            StatGroup.Stats[StatType.AccuracyToStatusSourcePercentAdjustment] = -_accuracyPenaltyPercent;
        }

        public int AccuracyPenaltyPercent => _accuracyPenaltyPercent;

        public override IStatusEffect Clone()
        {
            return new DecoyStatusEffect(_accuracyPenaltyPercent);
        }
    }
}

using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SmokeRoundStatusEffect : StatusEffectBase
    {
        private readonly int _accuracyPenaltyPercent;

        public override string Name => "Smoke Round";
        public override EffectIconType Icon => EffectIconType.SmokeBombStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public SmokeRoundStatusEffect()
            : this(20)
        {
        }

        public SmokeRoundStatusEffect(int accuracyPenaltyPercent)
        {
            _accuracyPenaltyPercent = Math.Max(0, accuracyPenaltyPercent);
            StatGroup.Stats[StatType.AccuracyPercentAdjustment] = -_accuracyPenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new SmokeRoundStatusEffect(_accuracyPenaltyPercent);
        }
    }
}

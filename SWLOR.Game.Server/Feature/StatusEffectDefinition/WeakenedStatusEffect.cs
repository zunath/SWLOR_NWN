using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class WeakenedStatusEffect : StatusEffectBase
    {
        private readonly int _attackPenaltyPercent;

        public override string Name => "Weakened";
        public override EffectIconType Icon => EffectIconType.AttackDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public WeakenedStatusEffect()
            : this(15)
        {
        }

        public WeakenedStatusEffect(int attackPenaltyPercent)
        {
            _attackPenaltyPercent = Math.Abs(attackPenaltyPercent);
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -_attackPenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new WeakenedStatusEffect(_attackPenaltyPercent);
        }
    }
}

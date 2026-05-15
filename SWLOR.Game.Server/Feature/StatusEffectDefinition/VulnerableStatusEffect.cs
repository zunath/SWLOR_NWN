using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class VulnerableStatusEffect : StatusEffectBase
    {
        private readonly int _defensePenaltyPercent;

        public override string Name => "Vulnerable";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public VulnerableStatusEffect()
            : this(10)
        {
        }

        public VulnerableStatusEffect(int defensePenaltyPercent)
        {
            _defensePenaltyPercent = Math.Abs(defensePenaltyPercent);
            StatGroup.Stats[StatType.DefensePercentAdjustment] = -_defensePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new VulnerableStatusEffect(_defensePenaltyPercent);
        }
    }
}

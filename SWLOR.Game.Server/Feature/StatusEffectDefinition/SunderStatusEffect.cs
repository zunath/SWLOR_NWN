using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SunderStatusEffect : StatusEffectBase
    {
        private readonly int _defensePenaltyPercent;

        public override string Name => "Sunder";
        public override EffectIconType Icon => EffectIconType.ACDecrease;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public SunderStatusEffect()
            : this(15)
        {
        }

        public SunderStatusEffect(int defensePenaltyPercent)
        {
            _defensePenaltyPercent = Math.Abs(defensePenaltyPercent);
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = -_defensePenaltyPercent;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = -_defensePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new SunderStatusEffect(_defensePenaltyPercent);
        }
    }
}

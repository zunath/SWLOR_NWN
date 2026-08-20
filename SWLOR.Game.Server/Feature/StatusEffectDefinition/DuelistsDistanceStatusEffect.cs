using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DuelistsDistanceStatusEffect : StatusEffectBase
    {
        private readonly int _damagePenaltyPercent;

        public override string Name => "Duelist's Distance";
        public override EffectIconType Icon => EffectIconType.DuelistsDistanceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.TreatmentKit1;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;

        public DuelistsDistanceStatusEffect()
            : this(10)
        {
        }

        public DuelistsDistanceStatusEffect(int damagePenaltyPercent)
        {
            _damagePenaltyPercent = Math.Abs(damagePenaltyPercent);
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = -_damagePenaltyPercent;
        }

        public override IStatusEffect Clone()
        {
            return new DuelistsDistanceStatusEffect(_damagePenaltyPercent);
        }
    }
}

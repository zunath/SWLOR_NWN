using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DisarmingShotStatusEffect : StatusEffectBase
    {
        private readonly int _attackPenalty;

        public override string Name => "Disarming Shot";
        public override EffectIconType Icon => EffectIconType.DisarmingShotStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.TreatmentKit1;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override bool PersistsOnLogout => false;

        public DisarmingShotStatusEffect()
            : this(5)
        {
        }

        public DisarmingShotStatusEffect(int attackPenalty)
        {
            _attackPenalty = Math.Abs(attackPenalty);
            StatGroup.Stats[StatType.AttackPercentAdjustment] = -_attackPenalty;
        }

        public override IStatusEffect Clone()
        {
            return new DisarmingShotStatusEffect(_attackPenalty);
        }
    }
}

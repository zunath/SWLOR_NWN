using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class FragmentationStatusEffect : StatusEffectBase
    {
        private readonly int _damage;
        private readonly float _frequency;

        public override string Name => "Fragmentation";
        public override EffectIconType Icon => EffectIconType.FragmentationStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit1 |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;
        public override float Frequency => _frequency;

        public FragmentationStatusEffect()
            : this(3, 3f)
        {
        }

        public FragmentationStatusEffect(int damage, float frequency)
        {
            _damage = Math.Max(1, damage);
            _frequency = Math.Max(1f, frequency);
        }

        public override IStatusEffect Clone()
        {
            return new FragmentationStatusEffect(_damage, _frequency);
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var amount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, _damage);
            amount = CombatDamageCalculator.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Physical);
            amount = CombatDamageCalculator.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Physical);
            if (amount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount), creature));
        }
    }
}

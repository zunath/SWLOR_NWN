using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SaturationTossStatusEffect : StatusEffectBase
    {
        private readonly int _damage;
        private readonly float _frequency;

        public override string Name => "Saturation Toss";
        public override EffectIconType Icon => EffectIconType.SaturationTossStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Fire;
        public override float Frequency => _frequency;

        public SaturationTossStatusEffect()
            : this(10, 4f)
        {
        }

        public SaturationTossStatusEffect(int damage, float frequency)
        {
            _damage = Math.Max(1, damage);
            _frequency = Math.Max(1f, frequency);
        }

        public override IStatusEffect Clone()
        {
            return new SaturationTossStatusEffect(_damage, _frequency);
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var amount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, _damage);
            amount = Combat.ApplyDamageOverTimeTakenModifiers(creature, amount, CombatDamageType.Fire);
            amount = Combat.ApplyDamageTakenModifiers(creature, amount, source, CombatDamageType.Fire);
            if (amount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Fire), creature));
        }
    }
}

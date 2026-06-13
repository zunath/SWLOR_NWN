using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ShockStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Shock";
        public override EffectIconType Icon => EffectIconType.ShockStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override ResistanceType ResistanceType => ResistanceType.Electrical;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        public ShockStatusEffect()
            : this(1)
        {
        }

        public ShockStatusEffect(int level)
        {
            _level = System.Math.Max(1, level);
        }

        public override IStatusEffect Clone()
        {
            return new ShockStatusEffect(_level);
        }

        protected override void Tick(uint creature)
        {
            var source = GetIsObjectValid(Source) ? Source : creature;
            var agility = GetAbilityModifier(AbilityType.Agility, source);
            var amount = System.Math.Max(1, d4() + agility * 2 * _level);
            amount = Resistance.ApplyResistanceToDamage(creature, ResistanceType, amount);
            if (amount <= 0)
                return;

            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Electrical), creature));
        }
    }
}

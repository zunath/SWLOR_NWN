using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BurnStatusEffect : StatusEffectBase
    {
        private readonly int _level;
        public override string Name => "Burn";
        public override EffectIconType Icon => EffectIconType.Burning;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        public BurnStatusEffect()
            : this(1)
        {
        }

        public BurnStatusEffect(int level)
        {
            _level = System.Math.Max(1, level);
        }

        public override IStatusEffect Clone()
        {
            return new BurnStatusEffect(_level);
        }

        protected override void Tick(uint creature)
        {
            var might = GetAbilityModifier(AbilityType.Might, Source);
            var amount = Random.Next(2, 4) + might * 2 * _level;
            ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Fire), creature);
        }
    }
}

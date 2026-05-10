using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PoisonStatusEffect : StatusEffectBase
    {
        public override string Name => "Poison";
        public override EffectIconType Icon => EffectIconType.Poison;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit1 |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 6f;

        protected override void Tick(uint creature)
        {
            var level = 1;
            var agility = GetAbilityModifier(AbilityType.Agility, Source);
            var amount = Random.Next(3, 7) + agility * level;
            ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, DamageType.Acid), creature);
            StatusEffect.ApplyStatusEffect(Source, creature, typeof(PoisonDefensePenaltyStatusEffect), 6f);
        }

        protected override void Remove(uint creature)
        {
            StatusEffect.RemoveStatusEffect(creature, typeof(PoisonDefensePenaltyStatusEffect), Source, false);
        }
    }
}

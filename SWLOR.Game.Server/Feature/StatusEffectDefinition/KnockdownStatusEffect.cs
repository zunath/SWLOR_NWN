using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KnockdownStatusEffect : StatusEffectBase
    {
        public override string Name => "Knockdown";
        public override EffectIconType Icon => EffectIconType.Fatigue;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyKnockdown(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyKnockdown(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyKnockdown(uint creature, float duration)
        {
            var effect = TagEffect(EffectKnockdown(), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
            Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Knockdown);
        }
    }
}

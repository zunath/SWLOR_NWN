using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KnockdownStatusEffect : StatusEffectBase
    {
        public override string Name => "Knockdown";
        public override EffectIconType Icon => EffectIconType.KnockdownStatusEffect;
        public override StatusEffectCategory Categories =>
            StatusEffectCategory.Debuff |
            StatusEffectCategory.Control |
            StatusEffectCategory.Incapacitating |
            StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already knocked down.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Knockdown)
                ? "Target is temporarily immune to knockdown."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyKnockdown(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyKnockdown(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Knockdown);
        }

        private void ApplyKnockdown(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectKnockdown());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

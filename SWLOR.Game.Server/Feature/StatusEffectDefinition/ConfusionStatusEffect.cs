using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ConfusionStatusEffect : StatusEffectBase
    {
        public override string Name => "Confusion";
        public override EffectIconType Icon => EffectIconType.ConfusionStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already confused.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Confused)
                ? "Target is temporarily immune to confusion."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyConfusion(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyConfusion(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Confused);
        }

        private void ApplyConfusion(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectConfused());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

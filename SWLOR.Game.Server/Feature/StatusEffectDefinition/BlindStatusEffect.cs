using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BlindStatusEffect : StatusEffectBase
    {
        public override string Name => "Blind";
        public override EffectIconType Icon => EffectIconType.BlindStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Trauma;

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already blind.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Blindness)
                ? "Target is temporarily immune to blindness."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyBlindness(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyBlindness(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Blindness);
        }

        private void ApplyBlindness(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectBlindness());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

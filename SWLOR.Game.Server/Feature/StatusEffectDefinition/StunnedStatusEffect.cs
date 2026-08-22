using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StunnedStatusEffect : StatusEffectBase
    {
        public override string Name => "Stunned";
        public override EffectIconType Icon => EffectIconType.StunnedStatusEffect;
        public override StatusEffectCategory Categories =>
            StatusEffectCategory.Control |
            StatusEffectCategory.Incapacitating |
            StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already stunned.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Stun)
                ? "Target is temporarily immune to stun."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyStun(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyStun(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Stun);
        }

        private void ApplyStun(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectStunned());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

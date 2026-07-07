using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DazedStatusEffect : StatusEffectBase
    {
        private readonly bool _grantsTemporaryImmunity;

        public override string Name => "Dazed";
        public override EffectIconType Icon => EffectIconType.DazedStatusEffect;
        public override StatusEffectCategory Categories =>
            StatusEffectCategory.Debuff |
            StatusEffectCategory.Control |
            StatusEffectCategory.Incapacitating;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public DazedStatusEffect()
            : this(true)
        {
        }

        public DazedStatusEffect(bool grantsTemporaryImmunity)
        {
            _grantsTemporaryImmunity = grantsTemporaryImmunity;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyDaze(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyDaze(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyDaze(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectDazed());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);

            if (_grantsTemporaryImmunity)
            {
                Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Dazed);
            }
        }

        public override IStatusEffect Clone()
        {
            return new DazedStatusEffect(_grantsTemporaryImmunity);
        }
    }
}

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
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
            StatusEffectCategory.Incapacitating |
            StatusEffectCategory.HardCrowdControl;
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

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already dazed.";

            return Stat.GetStatAdjustment(creature, StatType.DazeImmunity) > 0 ||
                   Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Dazed)
                ? "Target is temporarily immune to daze."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyDaze(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyDaze(creature, GetDurationSeconds(DurationTicks));
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            if (_grantsTemporaryImmunity)
            {
                Ability.ApplyPostControlImmunity(
                    creature,
                    SecondsSinceNaturalExpiration,
                    ImmunityType.Dazed);
            }
        }

        private void ApplyDaze(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectDazed());
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }

        public override IStatusEffect Clone()
        {
            return new DazedStatusEffect(_grantsTemporaryImmunity);
        }
    }
}

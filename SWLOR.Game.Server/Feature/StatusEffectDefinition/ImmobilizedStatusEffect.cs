using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImmobilizedStatusEffect : StatusEffectBase
    {
        public override string Name => "Immobilized";
        public override EffectIconType Icon => EffectIconType.ImmobilizedStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyImmobilize(creature, durationTicks);
        }

        protected override void Reapply(uint creature)
        {
            ApplyImmobilize(creature, DurationTicks);
        }

        public override string CanApply(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, GetType()))
                return "Target is already immobilized.";

            return Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Immobilized)
                ? "Target is temporarily immune to immobilization."
                : string.Empty;
        }

        protected override void Remove(uint creature)
        {
            if (IsBeingReplaced)
                return;

            if (GetIsObjectValid(creature) && !GetIsDead(creature))
            {
                Enmity.AttackHighestEnmityTarget(creature);
            }

            Ability.ApplyPostControlImmunity(
                creature,
                SecondsSinceNaturalExpiration,
                ImmunityType.Immobilized);
        }

        private void ApplyImmobilize(uint creature, int durationTicks)
        {
            var effect = TagNativeEffect(EffectCutsceneImmobilize());
            var durationType = durationTicks < 0
                ? DurationType.Permanent
                : DurationType.Temporary;
            var duration = GetDurationSeconds(durationTicks);
            ApplyEffectToObject(durationType, effect, creature, duration);
        }
    }
}

using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StunnedStatusEffect : StatusEffectBase
    {
        public override string Name => "Stunned";
        public override EffectIconType Icon => EffectIconType.Stunned;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyStun(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyStun(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyStun(uint creature, float duration)
        {
            var effect = TagEffect(EffectStunned(), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
            Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Stun);
        }
    }
}

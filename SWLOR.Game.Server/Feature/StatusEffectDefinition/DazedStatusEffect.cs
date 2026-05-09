using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class DazedStatusEffect : StatusEffectBase
    {
        public override string Name => "Dazed";
        public override EffectIconType Icon => EffectIconType.Dazed;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

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
            var effect = TagEffect(EffectDazed(), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
            Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.Dazed);
        }
    }
}

using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class BlindStatusEffect : StatusEffectBase
    {
        public override string Name => "Blind";
        public override EffectIconType Icon => EffectIconType.Blindness;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyBlindness(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyBlindness(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyBlindness(uint creature, float duration)
        {
            var effect = TagEffect(EffectBlindness(), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class MindTrickStatusEffect : StatusEffectBase
    {
        public override string Name => "Mind Trick";
        public override EffectIconType Icon => EffectIconType.Confused;
        public override ResistanceType ResistanceType => ResistanceType.Mind;

        public override string CanApply(uint creature)
        {
            var race = GetRacialType(creature);
            return race == RacialType.Cyborg ||
                   race == RacialType.Robot ||
                   race == RacialType.Droid
                ? "Mind trick does not work on this creature."
                : string.Empty;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyMindTrick(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyMindTrick(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyMindTrick(uint creature, float duration)
        {
            var effect = EffectConfused();
            effect = EffectLinkEffects(effect, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S));
            effect = TagEffect(effect, Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

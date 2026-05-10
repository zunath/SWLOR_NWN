using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ImmobilizedStatusEffect : StatusEffectBase
    {
        public override string Name => "Immobilized";
        public override EffectIconType Icon => EffectIconType.Entangle;
        public override StatusEffectCategory Categories => StatusEffectCategory.Control;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mobility;

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyImmobilize(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyImmobilize(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyImmobilize(uint creature, float duration)
        {
            var effect = TagEffect(EffectCutsceneImmobilize(), Id);
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

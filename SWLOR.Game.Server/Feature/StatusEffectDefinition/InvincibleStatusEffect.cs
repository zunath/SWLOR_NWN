using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InvincibleStatusEffect : StatusEffectBase
    {
        public override string Name => "Invincible";
        public override EffectIconType Icon => EffectIconType.InvincibleStatusEffect;

        public InvincibleStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -50;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyVisualEffect(creature, GetDurationSeconds(durationTicks));
        }

        protected override void Reapply(uint creature)
        {
            ApplyVisualEffect(creature, GetDurationSeconds(DurationTicks));
        }

        private void ApplyVisualEffect(uint creature, float duration)
        {
            var effect = TagNativeEffect(EffectVisualEffect(VisualEffect.Dur_Prot_Premonition));
            ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
        }
    }
}

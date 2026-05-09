using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class ForceRestoreStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override float Frequency => 6f;

        protected abstract bool RestoresFP { get; }
        protected abstract int Level { get; }

        protected override void Apply(uint creature, int durationTicks)
        {
            ForceRestore(creature);
        }

        protected override void Tick(uint creature)
        {
            ForceRestore(creature);
        }

        private void ForceRestore(uint target)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, Source) - 2;
            if (!RestoresFP)
            {
                willBonus -= 3;
            }

            if (willBonus < 0)
            {
                willBonus = 0;
            }

            var forceAmount = willBonus + Level * 2;
            var staminaAmount = willBonus + Level * 4;

            if (RestoresFP)
            {
                Stat.RestoreFP(target, forceAmount);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration_Lesser), target);
            }
            else
            {
                Stat.RestoreStamina(target, staminaAmount);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Holy), target);
            }
        }
    }
}

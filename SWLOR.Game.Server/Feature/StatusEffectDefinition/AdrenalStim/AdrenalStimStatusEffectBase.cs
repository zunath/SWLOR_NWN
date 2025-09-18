using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition.AdrenalStim
{
    public abstract class AdrenalStimStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override StatusEffectStackType StackingType => StatusEffectStackType.Disabled;
        public override float Frequency => 6f; // Tick every 6 seconds

        protected abstract int Level { get; }
        protected abstract string EffectName { get; }

        public override string Name => EffectName;

        protected override void Apply(uint creature, int durationTicks)
        {
            AdrenalStim(creature, Level);
        }

        protected override void Tick(uint creature)
        {
            AdrenalStim(creature, Level);
        }

        private void AdrenalStim(uint target, int level)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, Source) - 5;
            if (target == Source)
            {
                willBonus += level * 2;
            }
            if (willBonus <= 0)
            {
                willBonus = 0;
            }
            var staminaAmount = willBonus + level;
            if (staminaAmount < level)
            {
                staminaAmount = level;
            }

            Stat.RestoreStamina(target, staminaAmount);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
        }
    }
}

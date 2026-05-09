using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AdrenalStimStatusEffectBase : StatusEffectBase
    {
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override float Frequency => 6f;

        protected abstract int Level { get; }
        protected abstract string EffectName { get; }
        protected abstract Type WillpowerPenaltyStatusEffectClass { get; }

        public override string Name => EffectName;

        protected override void Apply(uint creature, int durationTicks)
        {
            AdrenalStim(creature);
            StatusEffect.ApplyStatusEffect(Source, Source, WillpowerPenaltyStatusEffectClass, GetDurationSeconds(durationTicks));
        }

        protected override void Tick(uint creature)
        {
            AdrenalStim(creature);
        }

        private void AdrenalStim(uint target)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, Source) - 5;
            if (target == Source)
            {
                willBonus += Level * 2;
            }

            if (willBonus <= 0)
            {
                willBonus = 0;
            }

            var staminaAmount = willBonus + Level;
            if (staminaAmount < Level)
            {
                staminaAmount = Level;
            }

            Stat.RestoreStamina(target, staminaAmount);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
        }
    }
}

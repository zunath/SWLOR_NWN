using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class AdrenalStimStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly IStatService _statService;

        public AdrenalStimStatusEffectDefinition(IStatService statService)
        {
            _statService = statService;
        }
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            AdrenalStim1(builder);
            AdrenalStim2(builder);
            AdrenalStim3(builder);

            return builder.Build();
        }

        private void AdrenalStim(uint source, uint target, int level)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, source) - 5;
            if (target == source)
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

            _statService.RestoreStamina(target, staminaAmount);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Head_Heal), target);
        }

        private void AdrenalStim1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.AdrenalStim1)
                .Name("Adrenal Stim I")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.AdrenalStim2, StatusEffectType.AdrenalStim3)
                .GrantAction((source, target, length, data) =>
                {
                    AdrenalStim(source, target, 1);
                })
                .TickAction((source, target, effectData) =>
                {
                    AdrenalStim(source, target, 1);
                });
        }

        private void AdrenalStim2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.AdrenalStim2)
                .Name("Adrenal Stim II")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.AdrenalStim3)
                .GrantAction((source, target, length, data) =>
                {
                    AdrenalStim(source, target, 2);
                })
                .TickAction((source, target, effectData) =>
                {
                    AdrenalStim(source, target, 2);
                });
        }

        private void AdrenalStim3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.AdrenalStim3)
                .Name("Adrenal Stim III")
                .EffectIcon(EffectIconType.Regenerate)
                .GrantAction((source, target, length, data) =>
                {
                    AdrenalStim(source, target, 3);
                })
                .TickAction((source, target, effectData) =>
                {
                    AdrenalStim(source, target, 3);
                });
        }
    }
}

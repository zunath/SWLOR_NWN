using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Model;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ForceRestoreStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly IStatService _statService;

        public ForceRestoreStatusEffectDefinition(IStatService statService)
        {
            _statService = statService;
        }
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForceBody1(builder);
            ForceBody2(builder);
            ForceMind1(builder);
            ForceMind2(builder);

            return builder.Build();
        }

        private void ForceRestore(uint source, uint target, bool forceBody, int level)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, source) - 2;
            if (!forceBody)
            {
                willBonus -= 3;
            }
            if (willBonus < 0)
            {
                willBonus = 0;
            }

            var forceAmount = willBonus + (level * 2);
            var staminaAmount = willBonus + (level * 4);

            if (forceBody)
            {
                _statService.RestoreFP(target, forceAmount);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration_Lesser), target);
            }
            else
            {
                _statService.RestoreStamina(target, staminaAmount);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Holy), target);
            }
        }

        private void ForceBody1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceBody1)
                .Name("Force Body I")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.ForceBody2)
                .GrantAction((source, target, length, data) =>
                {
                    ForceRestore(source, target, true, 1);
                })
                .TickAction((source, target, effectData) =>
                {
                    ForceRestore(source, target, true, 1);
                });
        }
        private void ForceBody2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceBody2)
                .Name("Force Body II")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.ForceBody2)
                .GrantAction((source, target, length, data) =>
                {
                    ForceRestore(source, target, true, 2);
                })
                .TickAction((source, target, effectData) =>
                {
                    ForceRestore(source, target, true, 2);
                });
        }

        private void ForceMind1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceMind1)
                .Name("Force Mind I")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.ForceMind1)
                .GrantAction((source, target, length, data) =>
                {
                    ForceRestore(source, target, false, 1);
                })
                .TickAction((source, target, effectData) =>
                {
                    ForceRestore(source, target, false, 1);
                });
        }
        private void ForceMind2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.ForceMind2)
                .Name("Force Mind II")
                .EffectIcon(EffectIconType.Regenerate)
                .CannotReplace(StatusEffectType.ForceMind2)
                .GrantAction((source, target, length, data) =>
                {
                    ForceRestore(source, target, false, 2);
                })
                .TickAction((source, target, effectData) =>
                {
                    ForceRestore(source, target, false, 2);
                });
        }
    }
}

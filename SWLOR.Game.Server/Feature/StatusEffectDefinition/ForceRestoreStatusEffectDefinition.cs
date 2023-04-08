using System.Collections.Generic;
using System.Reflection.Emit;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceRestoreStatusEffectDefinition : IStatusEffectListDefinition
    {
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
            int willBonus = GetAbilityScore(source, AbilityType.Willpower);

            int forceAmount = willBonus * level / 4;
            int staminaAmount = (2 * level) + willBonus * level / 4;

            if (forceBody == true)
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
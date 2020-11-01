using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BleedStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Bleed1(builder);
            Bleed2(builder);
            Bleed3(builder);

            return builder.Build();
        }

        private static void Bleed1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed1)
                .Name("Bleed I")
                .EffectIcon(157)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Bleed2) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Bleed3))
                    {
                        SendMessageToPC(target, "A more powerful version of Bleed is already in effect.");
                        StatusEffect.Remove(target, StatusEffectType.Bleed1);
                    }
                })
                .TickAction((activator, target) =>
                {
                    var damage = Random.D4(1);

                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    });
                });
        }

        private static void Bleed2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed2)
                .Name("Bleed II")
                .EffectIcon(158)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Bleed3))
                    {
                        SendMessageToPC(target, "A more powerful version of Bleed is already in effect.");
                        StatusEffect.Remove(target, StatusEffectType.Bleed2);
                        return;
                    }

                    StatusEffect.Remove(target, StatusEffectType.Bleed1);
                })
                .TickAction((activator, target) =>
                {
                    var damage = Random.D6(1);

                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    });
                });
        }

        private static void Bleed3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Bleed3)
                .Name("Bleed III")
                .EffectIcon(159)
                .GrantAction((target, duration) =>
                {
                    StatusEffect.Remove(target, StatusEffectType.Bleed1);
                    StatusEffect.Remove(target, StatusEffectType.Bleed3);
                })
                .TickAction((activator, target) =>
                {
                    var damage = Random.D6(2);

                    AssignCommand(activator, () =>
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Piercing), target);
                    });
                });
        }
    }
}

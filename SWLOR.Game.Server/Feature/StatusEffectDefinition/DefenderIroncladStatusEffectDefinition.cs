using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class DefenderIroncladStatusEffectDefinition: IStatusEffectListDefinition
    {
        private const string Defender1EffectTag = "DEFENDER_1_STATUS_EFFECT";
        private const string Defender2EffectTag = "DEFENDER_2_STATUS_EFFECT";
        private const string Defender3EffectTag = "DEFENDER_3_STATUS_EFFECT";
        private const string Ironclad1EffectTag = "IRONCLAD_1_STATUS_EFFECT";
        private const string Ironclad2EffectTag = "IRONCLAD_2_STATUS_EFFECT";
        private const string Ironclad3EffectTag = "IRONCLAD_3_STATUS_EFFECT";

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Defender1(builder);
            Defender2(builder);
            Defender3(builder);
            Ironclad1(builder);
            Ironclad2(builder);
            Ironclad3(builder);

            return builder.Build();
        }

        private static void ApplyDamageResistanceEffect(uint target, int amount, string effectTag, float duration)
        {
            var effect = EffectDamageReduction(amount, DamagePower.Normal);
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
        }

        private static void Defender1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Defender1)
                .Name("Defender I")
                .EffectIcon(140)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Defender2) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Defender3))
                    {
                        SendMessageToPC(target, "A more powerful version of Defender is already in effect.");
                        return;
                    }

                    RemoveEffectByTag(target, Defender1EffectTag);
                    ApplyDamageResistanceEffect(target, 3, Defender1EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Defender1EffectTag);
                });
        }

        private static void Defender2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Defender2)
                .Name("Defender II")
                .EffectIcon(141)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Defender3))
                    {
                        SendMessageToPC(target, "A more powerful version of Defender is already in effect.");
                        return;
                    }
                    RemoveEffectByTag(target, Defender1EffectTag, Defender2EffectTag);

                    ApplyDamageResistanceEffect(target, 5, Defender2EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Defender2EffectTag);
                });
        }

        private static void Defender3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Defender3)
                .Name("Defender III")
                .EffectIcon(142)
                .GrantAction((target, duration) =>
                {
                    RemoveEffectByTag(target, Defender1EffectTag, Defender2EffectTag, Defender3EffectTag);
                    ApplyDamageResistanceEffect(target, 7, Defender3EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Defender3EffectTag);
                });
        }

        private static void Ironclad1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Ironclad1)
                .Name("Ironclad I")
                .EffectIcon(143)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Ironclad2) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Ironclad3))
                    {
                        SendMessageToPC(target, "A more powerful version of Ironclad is already in effect.");
                        return;
                    }

                    RemoveEffectByTag(target, Ironclad1EffectTag);
                    ApplyDamageResistanceEffect(target, 4, Ironclad1EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Ironclad1EffectTag);
                });
        }

        private static void Ironclad2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Ironclad2)
                .Name("Ironclad II")
                .EffectIcon(144)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Ironclad3))
                    {
                        SendMessageToPC(target, "A more powerful version of Ironclad is already in effect.");
                        return;
                    }
                    RemoveEffectByTag(target, Ironclad1EffectTag, Ironclad2EffectTag);

                    ApplyDamageResistanceEffect(target, 6, Ironclad2EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Ironclad2EffectTag);
                });
        }

        private static void Ironclad3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Ironclad3)
                .Name("Ironclad III")
                .EffectIcon(145)
                .GrantAction((target, duration) =>
                {
                    RemoveEffectByTag(target, Ironclad1EffectTag, Ironclad2EffectTag, Ironclad3EffectTag);
                    ApplyDamageResistanceEffect(target, 8, Ironclad3EffectTag, 60f);
                })
                .RemoveAction(target =>
                {
                    RemoveEffectByTag(target, Ironclad3EffectTag);
                });
        }
    }
}

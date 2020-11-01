using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class CoverStatusEffectDefinition : IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Cover1(builder);
            Cover2(builder);
            Cover3(builder);
            Cover4(builder);

            return builder.Build();
        }

        /// <summary>
        /// When the On Damage event fires, if the creature has any of the "Cover" effects active,
        /// the source of the Cover effect will receive an appropriate percentage of the damage received.
        /// </summary>
        [NWNEventHandler("on_nwnx_dmg")]
        public static void HandleCoverStatusEffect()
        {
            int CalculateAndApplyAdjustedDamage(uint source, int amount, float reductionPercent, DamageType damageType)
            {
                // -1 is returned so that the Damage plugin doesn't report it in the combat log.
                if (amount <= 0) return -1;
                if (reductionPercent <= 0.0f) return -1;

                var sourceDamage = (int)(amount * reductionPercent);

                // Always remove at least one point of damage from the covered creature.
                if (sourceDamage <= 0)
                {
                    sourceDamage = 1;
                    amount--;
                }
                // Otherwise remove a percentage of the damage from the source.
                else
                {
                    amount = (int) (amount * (1f - reductionPercent));
                }

                ApplyEffectToObject(DurationType.Instant, EffectDamage(sourceDamage, damageType), source);
                return amount;
            }

            var target = OBJECT_SELF;
            uint source;
            float reductionPercent;

            if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover1))
            {
                reductionPercent = 10f;
                source = StatusEffect.GetSource(target, StatusEffectType.Cover1);
            }
            else if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover2))
            {
                reductionPercent = 15f;
                source = StatusEffect.GetSource(target, StatusEffectType.Cover2);
            }
            else if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover3))
            {
                reductionPercent = 20f;
                source = StatusEffect.GetSource(target, StatusEffectType.Cover3);
            }
            else if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover4))
            {
                reductionPercent = 25f;
                source = StatusEffect.GetSource(target, StatusEffectType.Cover4);
            }
            else return;

            if (!GetIsObjectValid(source))
                return;

            var damageDetails = Damage.GetDamageEventData();
            damageDetails.Bludgeoning = CalculateAndApplyAdjustedDamage(source, damageDetails.Bludgeoning, reductionPercent, DamageType.Bludgeoning);
            damageDetails.Pierce = CalculateAndApplyAdjustedDamage(source, damageDetails.Pierce, reductionPercent, DamageType.Piercing);
            damageDetails.Slash = CalculateAndApplyAdjustedDamage(source, damageDetails.Slash, reductionPercent, DamageType.Slashing);
            damageDetails.Acid = CalculateAndApplyAdjustedDamage(source, damageDetails.Magical, reductionPercent, DamageType.Acid);
            damageDetails.Cold = CalculateAndApplyAdjustedDamage(source, damageDetails.Cold, reductionPercent, DamageType.Cold);
            damageDetails.Divine = CalculateAndApplyAdjustedDamage(source, damageDetails.Divine, reductionPercent, DamageType.Divine);
            damageDetails.Electrical = CalculateAndApplyAdjustedDamage(source, damageDetails.Electrical, reductionPercent, DamageType.Electrical);
            damageDetails.Fire = CalculateAndApplyAdjustedDamage(source, damageDetails.Fire, reductionPercent, DamageType.Fire);
            damageDetails.Negative = CalculateAndApplyAdjustedDamage(source, damageDetails.Negative, reductionPercent, DamageType.Negative);
            damageDetails.Positive = CalculateAndApplyAdjustedDamage(source, damageDetails.Positive, reductionPercent, DamageType.Positive);
            damageDetails.Sonic = CalculateAndApplyAdjustedDamage(source, damageDetails.Sonic, reductionPercent, DamageType.Sonic);
            damageDetails.Base = CalculateAndApplyAdjustedDamage(source, damageDetails.Base, reductionPercent, DamageType.BaseWeapon);

            Damage.SetDamageEventData(damageDetails);
        }

        private static void Cover1(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Cover1)
                .Name("Cover I")
                .EffectIcon(148)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover2) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover3) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover4))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover1);
                        SendMessageToPC(target, "A more powerful Cover effect is already active.");
                    }
                });
        }

        private static void Cover2(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Cover2)
                .Name("Cover II")
                .EffectIcon(149)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover1))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover1);
                    }

                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover3) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover4))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover2);
                        SendMessageToPC(target, "A more powerful Cover effect is already active.");
                    }
                });
        }

        private static void Cover3(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Cover3)
                .Name("Cover III")
                .EffectIcon(150)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover1) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover2))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover1);
                        StatusEffect.Remove(target, StatusEffectType.Cover2);
                    }

                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover4))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover3);
                        SendMessageToPC(target, "A more powerful Cover effect is already active.");
                    }
                });
        }

        private static void Cover4(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Cover4)
                .Name("Cover IV")
                .EffectIcon(151)
                .GrantAction((target, duration) =>
                {
                    if (StatusEffect.HasStatusEffect(target, StatusEffectType.Cover1) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover2) ||
                        StatusEffect.HasStatusEffect(target, StatusEffectType.Cover3))
                    {
                        StatusEffect.Remove(target, StatusEffectType.Cover1);
                        StatusEffect.Remove(target, StatusEffectType.Cover2);
                        StatusEffect.Remove(target, StatusEffectType.Cover3);
                    }
                });
        }
    }
}
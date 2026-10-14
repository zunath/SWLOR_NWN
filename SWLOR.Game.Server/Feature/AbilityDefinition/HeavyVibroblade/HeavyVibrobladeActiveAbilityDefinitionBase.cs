using SWLOR.Game.Server.Core;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public abstract class HeavyVibrobladeActiveAbilityDefinitionBase
    {
        protected static bool ToggleSelfStatus(uint activator, Type type)
        {
            if (!StatusEffect.HasStatusEffect(activator, type))
                return true;

            StatusEffect.RemoveStatusEffect(activator, type, false);
            SendMessageToPC(activator, $"{StatusEffect.GetStatusEffectName(type)} deactivated.");
            return false;
        }

        protected static void ApplySelfStatus(uint activator, Type type)
        {
            StatusEffect.RemoveOtherStanceStatuses(activator, type);
            if (StatusEffect.ApplyStatusEffect(activator, activator, type, 0f))
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        protected static int SoulStrikeImpact(uint activator, uint target, Location targetLocation, int damageBonus, int healingPercent)
        {
            using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(activator, target);
            var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, damageBonus, 0, null, false);
            if (damage > 0)
            {
                ApplyEssenceHunter(activator, target);
            }

            HealFromDamage(activator, damage, healingPercent);
            return damage;
        }

        protected static void ApplyEssenceHunter(uint activator, uint target)
        {
            if (!GetIsObjectValid(target) ||
                Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseEssenceHunter) <= 0)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(EssenceDrainStatusEffect), 30f);
        }

        protected static void HealFromDamage(uint target, int damage, int healingPercent)
        {
            var amount = Combat.ApplyDamageDerivedHealing(
                target,
                damage,
                healingPercent,
                isActivatedHealing: true);
            if (amount > 0)
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
        }

        protected static void SacrificeHitPoints(uint activator, int percent)
        {
            SacrificeHitPoints(activator, percent, percent);
        }

        protected static void SacrificeHitPoints(uint activator, int basePercent, int minimumPercent)
        {
            var percent = GetHitPointCostPercent(activator, basePercent, minimumPercent);
            var cost = GameMath.PercentOf(GetMaxHitPoints(activator), percent);
            // The payment stops at 1 HP. Temporary HP absorbs it first, so riders such as Bloodlust
            // scale with the HP actually lost.
            var payment = Math.Min(GetCurrentHitPoints(activator) - 1, cost);
            if (payment <= 0)
                return;

            var hitPointsSpent = Combat.CalculateHitPointsSpentThroughTemporaryHitPoints(
                payment,
                TemporaryHitPointEffects.GetRemaining(activator));
            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(payment), activator);
                Combat.ApplyLowHPDamageTakenEffects(activator, payment);
            });
            Combat.ApplyHitPointSpendAbilityEffects(activator, hitPointsSpent, cost);
        }

        /// <summary>HP-cost abilities can't be used at 1 HP, where there is nothing left to pay.</summary>
        protected static string ValidateCanSpendHitPoints(uint activator)
        {
            return GetCurrentHitPoints(activator) > 1
                ? string.Empty
                : "You need more than 1 HP to use this ability.";
        }

        protected static int GetHitPointCostPercent(uint activator, int basePercent, int minimumPercent)
        {
            return CalculateHitPointCostPercent(basePercent, minimumPercent,
                GetAbilityScore(activator, AbilityType.Might));
        }

        protected static int CalculateHitPointCostPercent(int basePercent, int minimumPercent, int might)
        {
            return HeavyVibrobladeMightCostRules.Percent(basePercent, minimumPercent, might);
        }

        protected static void ApplyStatusToNearbyParty(
            uint activator,
            Type type,
            float duration,
            bool includeSelf,
            VisualEffect visualEffect = VisualEffect.None)
        {
            if (includeSelf)
            {
                if (StatusEffect.ApplyStatusEffect(activator, activator, type, duration))
                    Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
                ApplyVisualEffect(activator, visualEffect);
            }

            var location = GetLocation(activator);
            foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, 5f, location, true))
            {
                if (!GetIsObjectValid(creature))
                    continue;

                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    if (StatusEffect.ApplyStatusEffect(activator, creature, type, duration))
                        Ability.PlaySuccessfulImpactVisualEffect(activator, creature);
                    ApplyVisualEffect(creature, visualEffect);
                }

            }
        }

        private static void ApplyVisualEffect(uint target, VisualEffect visualEffect)
        {
            if (visualEffect == VisualEffect.None)
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), target);
        }

        protected static void ApplyImmunityToNearbyParty(uint activator, ImmunityType immunity, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                Ability.ApplyTemporaryImmunity(activator, duration, immunity);
            }

            var location = GetLocation(activator);
            foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, 5f, location, true))
            {
                if (!GetIsObjectValid(creature))
                    continue;

                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    Ability.ApplyTemporaryImmunity(creature, duration, immunity);
                }

            }
        }
    }
}

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
            StatusEffect.ApplyStatusEffect(activator, activator, type, 0f);
        }

        protected static int SoulStrikeImpact(uint activator, uint target, Location targetLocation, int damageBonus, int healingPercent)
        {
            using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(activator);
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
                applyCombatReadiness: true);
            if (amount > 0)
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Negative_Energy), target);
        }

        protected static void SacrificeHitPoints(uint activator, int percent)
        {
            SacrificeHitPoints(activator, percent, percent);
        }

        protected static void SacrificeHitPoints(uint activator, int basePercent, int minimumPercent)
        {
            var percent = Math.Max(minimumPercent, basePercent - Math.Max(0, GetAbilityScore(activator, AbilityType.Might)));
            var amount = GameMath.PercentOf(GetMaxHitPoints(activator), percent);
            var currentHp = GetCurrentHitPoints(activator);

            if (currentHp <= 1)
                return;

            amount = Math.Min(currentHp - 1, amount);
            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(amount), activator);
                Combat.ApplyLowHPDamageTakenEffects(activator, amount);
            });
            Combat.ApplyHitPointSpendAbilityEffects(activator);
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
                StatusEffect.ApplyStatusEffect(activator, activator, type, duration);
                ApplyVisualEffect(activator, visualEffect);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration);
                    ApplyVisualEffect(creature, visualEffect);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
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
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    Ability.ApplyTemporaryImmunity(creature, duration, immunity);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
        }
    }
}

using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
            StatusEffect.ApplyStatusEffect(activator, activator, type, 0f);
        }

        protected static int SoulStrikeImpact(uint activator, uint target, Location targetLocation, int damageBonus, int healingPercent)
        {
            var damage = Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, damageBonus, 0, null, false);
            HealFromDamage(activator, damage, healingPercent);
            return damage;
        }

        protected static void HealFromDamage(uint target, int damage, int healingPercent)
        {
            if (damage <= 0 || healingPercent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(damage * (healingPercent / 100f)));
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        protected static void SacrificeHitPoints(uint activator, int percent)
        {
            SacrificeHitPoints(activator, percent, percent);
        }

        protected static void SacrificeHitPoints(uint activator, int basePercent, int minimumPercent)
        {
            var percent = Math.Max(minimumPercent, basePercent - Math.Max(0, GetAbilityScore(activator, AbilityType.Might)));
            var amount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * (percent / 100f)));
            var currentHp = GetCurrentHitPoints(activator);

            if (currentHp <= 1)
                return;

            amount = Math.Min(currentHp - 1, amount);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount), activator));
        }

        protected static void ApplyStatusToNearbyParty(uint activator, Type type, float duration, bool includeSelf)
        {
            if (includeSelf)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, type, duration);
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, 5f, location, true);

            while (GetIsObjectValid(creature))
            {
                if (creature != activator && Party.IsInParty(activator, creature))
                {
                    StatusEffect.ApplyStatusEffect(activator, creature, type, duration);
                }

                creature = GetNextObjectInShape(Shape.Sphere, 5f, location, true);
            }
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

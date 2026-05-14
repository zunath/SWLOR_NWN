using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class DeviceAbilityEffects
    {
        public static int ApplyCapacitorRigBonus(uint activator, int amount)
        {
            var bonusPercent = Stat.GetStatAdjustment(activator, StatType.DeviceShieldTemporaryHPPercentAdjustment);
            return amount + amount * bonusPercent / 100;
        }

        public static float ApplyCapacitorRigDurationBonus(uint activator, float durationSeconds)
        {
            return durationSeconds + Stat.GetStatAdjustment(activator, StatType.DeviceShieldDurationBonusSeconds);
        }

        public static float ApplyGrenadeRadiusBonus(uint activator, float baseRadius)
        {
            return baseRadius + Stat.GetStatAdjustment(activator, StatType.GrenadeRadiusBonusTenths) / 10f;
        }

        public static int ApplyGrenadeControlPotencyBonus(uint activator, int baseAdjustment)
        {
            var bonus = Stat.GetStatAdjustment(activator, StatType.GrenadeControlPotencyBonus);
            if (baseAdjustment == 0 || bonus == 0)
                return baseAdjustment;

            return baseAdjustment < 0
                ? baseAdjustment - bonus
                : baseAdjustment + bonus;
        }

        public static void ScheduleSingleHostilePulses(
            uint activator,
            Location location,
            SkillType skillType,
            int baseDamage,
            int statusDuration,
            Type statusEffect,
            float radius,
            float durationSeconds,
            CombatDamageType damageType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect = VisualEffect.None)
        {
            for (var elapsed = 3f; elapsed <= durationSeconds + 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    var target = GetNearestHostileCreature(activator, location, radius);
                    if (!GetIsObjectValid(target))
                        return;

                    if (areaVisualEffect != VisualEffect.None)
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(areaVisualEffect), location);

                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        skillType,
                        baseDamage,
                        statusDuration,
                        statusEffect,
                        false,
                        Array.Empty<Type>(),
                        damageType: damageType,
                        targetVisualEffect: targetVisualEffect);
                });
            }
        }

        public static void ScheduleAreaHostilePulses(
            uint activator,
            Location location,
            SkillType skillType,
            int baseDamage,
            int statusDuration,
            Type statusEffect,
            float radius,
            float durationSeconds,
            CombatDamageType damageType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect = VisualEffect.None)
        {
            for (var elapsed = 3f; elapsed <= durationSeconds + 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        OBJECT_INVALID,
                        location,
                        skillType,
                        baseDamage,
                        statusDuration,
                        statusEffect,
                        CombatImpactAreaShape.Sphere,
                        0f,
                        radius,
                        0f,
                        Array.Empty<Type>(),
                        damageType: damageType,
                        targetVisualEffect: targetVisualEffect,
                        areaVisualEffect: areaVisualEffect);
                });
            }
        }

        public static void ScheduleFriendlyZoneStatus(
            uint activator,
            Location location,
            float radius,
            float durationSeconds,
            Type statusEffect,
            VisualEffect visualEffect = VisualEffect.None)
        {
            for (var elapsed = 0f; elapsed < durationSeconds - 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, radius))
                    {
                        StatusEffect.ApplyStatusEffect(activator, friendly, statusEffect, 3.2f);
                        if (visualEffect != VisualEffect.None)
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), friendly);
                    }
                });
            }
        }

        public static void ScheduleFriendlyZoneHealing(
            uint activator,
            Location location,
            float radius,
            float durationSeconds,
            float percentPerTick,
            Type statusEffect = null,
            VisualEffect visualEffect = VisualEffect.None)
        {
            for (var elapsed = 3f; elapsed <= durationSeconds + 0.01f; elapsed += 3f)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, radius))
                    {
                        AbilityEffectScaling.ApplyScaledHeal(activator, friendly, percentPerTick);

                        if (statusEffect != null)
                            StatusEffect.ApplyStatusEffect(activator, friendly, statusEffect, 3.2f);

                        if (visualEffect != VisualEffect.None)
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), friendly);
                    }
                });
            }
        }

        private static uint GetNearestHostileCreature(uint activator, Location location, float radius)
        {
            if (!GetIsObjectValid(activator) ||
                GetCurrentHitPoints(activator) <= 0 ||
                !GetIsObjectValid(GetAreaFromLocation(location)))
            {
                return OBJECT_INVALID;
            }

            var nth = 1;
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            while (GetIsObjectValid(creature) &&
                   GetDistanceBetweenLocations(location, GetLocation(creature)) <= radius)
            {
                if (GetIsReactionTypeHostile(creature, activator))
                    return creature;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }

            return OBJECT_INVALID;
        }
    }
}

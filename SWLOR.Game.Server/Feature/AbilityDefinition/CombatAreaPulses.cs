using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class CombatAreaPulses
    {
        public static void SchedulePulses(
            uint activator,
            Location location,
            float durationSeconds,
            float intervalSeconds,
            bool centerOnActivator,
            Action<Location> pulseAction)
        {
            if (pulseAction == null)
                return;

            SchedulePulses(
                activator,
                location,
                durationSeconds,
                intervalSeconds,
                centerOnActivator,
                (pulseLocation, _) => pulseAction(pulseLocation));
        }

        public static void SchedulePulses(
            uint activator,
            Location location,
            float durationSeconds,
            float intervalSeconds,
            bool centerOnActivator,
            Action<Location, float> pulseAction)
        {
            if (durationSeconds <= 0f || intervalSeconds <= 0f || pulseAction == null)
                return;

            for (var elapsed = intervalSeconds; elapsed <= durationSeconds + 0.01f; elapsed += intervalSeconds)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                        return;

                    var pulseLocation = centerOnActivator ? GetLocation(activator) : location;
                    if (!GetIsObjectValid(GetAreaFromLocation(pulseLocation)))
                        return;

                    pulseAction(pulseLocation, pulseDelay);
                });
            }
        }

        public static void ApplyCombatPulse(
            uint activator,
            Location location,
            SkillType skillType,
            int baseDamage,
            float radius,
            Type statusEffect = null,
            int statusDurationSeconds = 0,
            CombatDamageType damageType = CombatDamageType.Physical,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            bool alwaysApplyAreaVisualEffect = true,
            Action<uint> afterSuccessfulHit = null)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                OBJECT_INVALID,
                location,
                skillType,
                baseDamage,
                statusDurationSeconds,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                0f,
                radius,
                damageType: damageType,
                targetVisualEffect: targetVisualEffect,
                areaVisualEffect: areaVisualEffect,
                alwaysApplyAreaVisualEffect: alwaysApplyAreaVisualEffect,
                afterSuccessfulHit: afterSuccessfulHit);
        }

        public static int CountHostileCreatures(uint activator, Location location, float radius)
        {
            var count = 0;
            foreach (var _ in GetHostileCreatures(activator, location, radius))
            {
                count++;
            }

            return count;
        }

        public static IEnumerable<uint> GetHostileCreatures(uint activator, Location location, float radius)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(GetAreaFromLocation(location)))
                yield break;

            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            while (GetIsObjectValid(creature))
            {
                if (GetCurrentHitPoints(creature) > 0 &&
                    !GetIsDead(creature) &&
                    GetIsReactionTypeHostile(creature, activator))
                {
                    yield return creature;
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }
        }
    }
}

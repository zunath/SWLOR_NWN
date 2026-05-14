using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    public static class AbilityTargeting
    {
        private const float DefaultFriendlyTargetRadius = 5f;

        public static string ValidateFriendlyTarget(
            uint activator,
            uint target,
            bool allowSelf = true,
            bool requireDead = false)
        {
            if (!GetIsObjectValid(target))
                return "A friendly target is required.";

            if (!allowSelf && target == activator)
                return "You cannot target yourself with this ability.";

            if (GetIsReactionTypeHostile(target, activator))
                return "You may only use this ability on allies.";

            var isDead = GetIsDead(target) || GetCurrentHitPoints(target) <= 0;
            if (requireDead && !isDead)
                return "Your target is not unconscious.";

            if (!requireDead && isDead)
                return "Your target must be alive.";

            return string.Empty;
        }

        public static uint ResolveFriendlyTarget(uint activator, uint target, bool allowSelf = true)
        {
            if (GetIsObjectValid(target) &&
                !GetIsReactionTypeHostile(target, activator) &&
                (allowSelf || target != activator))
            {
                return target;
            }

            return allowSelf ? activator : OBJECT_INVALID;
        }

        public static IEnumerable<uint> GetFriendlyTargets(
            uint activator,
            uint target,
            bool affectsParty,
            float radius = DefaultFriendlyTargetRadius)
        {
            if (!affectsParty)
            {
                if (GetIsObjectValid(target) &&
                    !GetIsReactionTypeHostile(target, activator) &&
                    !GetIsDead(target) &&
                    GetCurrentHitPoints(target) > 0)
                {
                    yield return target;
                    yield break;
                }

                if (!GetIsDead(activator) && GetCurrentHitPoints(activator) > 0)
                    yield return activator;

                yield break;
            }

            var location = GetLocation(activator);
            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            var yieldedActivator = false;

            while (GetIsObjectValid(creature))
            {
                if (creature == activator || Party.IsInParty(activator, creature))
                {
                    if (creature == activator)
                        yieldedActivator = true;

                    if (!GetIsDead(creature) && GetCurrentHitPoints(creature) > 0)
                        yield return creature;
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }

            if (!yieldedActivator && !GetIsDead(activator) && GetCurrentHitPoints(activator) > 0)
                yield return activator;
        }

        public static IEnumerable<uint> GetFriendlyTargetsNearLocation(
            uint activator,
            Location location,
            float radius,
            bool includeActivator = true)
        {
            if (!GetIsObjectValid(GetAreaFromLocation(location)))
                location = GetLocation(activator);

            var creature = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            var yieldedActivator = false;

            while (GetIsObjectValid(creature))
            {
                if ((creature == activator && includeActivator) || Party.IsInParty(activator, creature))
                {
                    if (creature == activator)
                        yieldedActivator = true;

                    if (!GetIsDead(creature) && GetCurrentHitPoints(creature) > 0)
                        yield return creature;
                }

                creature = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }

            if (includeActivator &&
                !yieldedActivator &&
                IsAlive(activator) &&
                IsInsideLocationRadius(activator, location, radius))
            {
                yield return activator;
            }
        }

        public static IEnumerable<uint> GetHostileTargetsNearLocation(
            uint activator,
            Location location,
            float radius,
            int maxTargets,
            uint priorityTarget = OBJECT_INVALID,
            Func<uint, bool> predicate = null)
        {
            if (!GetIsObjectValid(GetAreaFromLocation(location)))
                location = GetLocation(activator);

            var yieldedTargets = new HashSet<uint>();
            var yieldedCount = 0;

            if (IsValidHostileTarget(activator, priorityTarget, location, radius, predicate))
            {
                yieldedTargets.Add(priorityTarget);
                yieldedCount++;
                yield return priorityTarget;
            }

            var nth = 1;
            var creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);

            while (GetIsObjectValid(creature) && GetDistanceBetweenLocations(location, GetLocation(creature)) <= radius)
            {
                if ((maxTargets <= 0 || yieldedCount < maxTargets) &&
                    !yieldedTargets.Contains(creature) &&
                    IsValidHostileTarget(activator, creature, location, radius, predicate))
                {
                    yieldedTargets.Add(creature);
                    yieldedCount++;
                    yield return creature;
                }

                if (maxTargets > 0 && yieldedCount >= maxTargets)
                    yield break;

                nth++;
                creature = GetNearestCreatureToLocation(CreatureType.IsAlive, true, location, nth);
            }
        }

        public static Location ResolveImpactLocation(uint activator, uint target, Location targetLocation)
        {
            if (GetIsObjectValid(target))
                return GetLocation(target);

            return GetIsObjectValid(GetAreaFromLocation(targetLocation))
                ? targetLocation
                : GetLocation(activator);
        }

        private static bool IsAlive(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   !GetIsDead(creature) &&
                   GetCurrentHitPoints(creature) > 0;
        }

        private static bool IsValidHostileTarget(uint activator, uint target, Location location, float radius, Func<uint, bool> predicate)
        {
            return IsAlive(target) &&
                   GetIsReactionTypeHostile(target, activator) &&
                   IsInsideLocationRadius(target, location, radius) &&
                   (predicate == null || predicate(target));
        }

        private static bool IsInsideLocationRadius(uint creature, Location location, float radius)
        {
            return GetArea(creature) == GetAreaFromLocation(location) &&
                   GetDistanceBetweenLocations(GetLocation(creature), location) <= radius;
        }
    }
}

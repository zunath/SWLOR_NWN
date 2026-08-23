using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CompanionControlService
{
    public static class CompanionControlPolicy
    {
        public const float FollowTetherMeters = 15f;
        public const float GuardTetherMeters = 8f;
        public const float AttackNearestRangeMeters = 15f;
        public const float PathingTimeoutSeconds = 5f;
        public const float ProgressDistanceMeters = 0.25f;

        public static float GetTetherMeters(CompanionMode mode, CompanionEngagementType engagementType)
        {
            if (engagementType == CompanionEngagementType.AttackNearest)
                return AttackNearestRangeMeters;

            return mode == CompanionMode.Guard
                ? GuardTetherMeters
                : FollowTetherMeters;
        }

        public static bool HasPathingTimedOut(DateTime lastProgressAt, DateTime now)
        {
            return lastProgressAt != default &&
                   (now - lastProgressAt).TotalSeconds >= PathingTimeoutSeconds;
        }

        public static bool HasCombatProgress(
            bool hasNewOffensiveActivity,
            float previousDistance,
            float currentDistance)
        {
            return hasNewOffensiveActivity ||
                   currentDistance + ProgressDistanceMeters < previousDistance;
        }

        public static bool ShouldPreserveExplicitOrder(
            DateTime expiresAt,
            DateTime now,
            ActionType currentAction)
        {
            return expiresAt > now &&
                   currentAction is not ActionType.Invalid and not ActionType.Follow;
        }

        public static bool ShouldStopActionInStandGround(ActionType currentAction)
        {
            return currentAction is not ActionType.Invalid and
                   not ActionType.Wait and
                   not ActionType.Sit and
                   not ActionType.CastSpell and
                   not ActionType.ItemCastSpell and
                   not ActionType.CounterSpell;
        }

        public static bool ReturnsToFollowWhenComplete(CompanionEngagementType engagementType)
        {
            return engagementType == CompanionEngagementType.AttackNearest;
        }

        public static bool IsWithinAreaReach(float distance, float reach)
        {
            return reach > 0f && distance <= reach;
        }

        public static bool IsWithinSelfOriginAreaReach(
            AbilityTargetingShapeType shape,
            bool originOnSelf,
            float distance,
            float sizeX)
        {
            return originOnSelf &&
                   shape is (AbilityTargetingShapeType.Sphere or
                       AbilityTargetingShapeType.HSphere or
                       AbilityTargetingShapeType.Rect or
                       AbilityTargetingShapeType.Cone) &&
                   IsWithinAreaReach(distance, sizeX);
        }
    }
}

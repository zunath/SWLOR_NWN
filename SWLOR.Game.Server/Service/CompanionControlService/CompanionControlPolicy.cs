namespace SWLOR.Game.Server.Service.CompanionControlService
{
    public static class CompanionControlPolicy
    {
        public const float FollowTetherMeters = 15f;
        public const float GuardTetherMeters = 8f;
        public const float AttackNearestRangeMeters = 15f;
        public const float PathingTimeoutSeconds = 5f;

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

        public static bool ReturnsToFollowWhenComplete(CompanionEngagementType engagementType)
        {
            return engagementType == CompanionEngagementType.AttackNearest;
        }
    }
}

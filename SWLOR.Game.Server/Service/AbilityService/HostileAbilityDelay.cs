namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// A short wait every hostile ability shares, so a hotbar of instant abilities can't all land at
    /// the same moment. Abilities that aren't hostile neither start it nor wait for it.
    /// </summary>
    public static class HostileAbilityDelay
    {
        public const float Seconds = 0.5f;

        private const string ReadyAtVariable = "HOSTILE_ABILITY_READY_AT";

        /// <summary>Seconds left before the creature can use another hostile ability, or 0 when it can.</summary>
        public static float GetRemainingSeconds(uint creature)
        {
            if (GetIsDM(creature) ||
                !RecastTimestamp.TryParse(GetLocalString(creature, ReadyAtVariable), out var readyAt))
            {
                return 0f;
            }

            var remaining = (float)(readyAt - DateTime.UtcNow).TotalSeconds;
            return remaining > 0f ? remaining : 0f;
        }

        public static void Start(uint creature)
        {
            SetLocalString(creature, ReadyAtVariable, RecastTimestamp.Format(DateTime.UtcNow.AddSeconds(Seconds)));
        }

        /// <summary>Ends the creature's delay at once. For tests only.</summary>
        public static void EndForTesting(uint creature)
        {
            DeleteLocalString(creature, ReadyAtVariable);
        }
    }
}

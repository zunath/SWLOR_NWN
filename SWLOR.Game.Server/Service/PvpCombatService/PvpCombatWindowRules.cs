namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>
    /// The PvP combat window (Player City Revamp, "Conflict and PvP"), with no engine or database calls. Any PvP damage a
    /// player deals or takes holds their window open for <see cref="WindowSeconds"/>. While it is open the player cannot
    /// turn the PvP flag off or change the identity they show: the request waits and takes effect when the window ends, so
    /// nobody escapes a bounty or gives away a disguise mid-fight. Turning the flag on is never held back. This is separate
    /// from <see cref="PvpCombatRules"/>, whose 30-second fight clock drives the healing and damage ramps.
    /// </summary>
    public static class PvpCombatWindowRules
    {
        /// <summary>How long after any PvP damage dealt or taken the window stays open.</summary>
        public const double WindowSeconds = 60.0;

        /// <summary>When the window closes after PvP damage at <paramref name="nowUtc"/>, never earlier than it already would.</summary>
        public static DateTime Extend(DateTime? currentUntilUtc, DateTime nowUtc)
        {
            var until = nowUtc.AddSeconds(WindowSeconds);
            return currentUntilUtc.HasValue && currentUntilUtc.Value > until ? currentUntilUtc.Value : until;
        }

        public static bool IsOpen(DateTime? untilUtc, DateTime nowUtc)
        {
            return untilUtc.HasValue && untilUtc.Value > nowUtc;
        }

        /// <summary>Whole seconds until the window closes, or 0 when it is closed.</summary>
        public static int GetSecondsLeft(DateTime? untilUtc, DateTime nowUtc)
        {
            return IsOpen(untilUtc, nowUtc) ? (int)Math.Ceiling((untilUtc.Value - nowUtc).TotalSeconds) : 0;
        }

        /// <summary>
        /// Decides a request to turn the PvP flag on or off for the identity the player shows, and records a waiting
        /// request in <paramref name="requests"/>. Turning it on goes ahead at once and undoes a waiting off request.
        /// </summary>
        /// <param name="windowOpen">Whether the player's window is open.</param>
        /// <param name="flagIsOn">Whether the flag is in effect now.</param>
        /// <param name="wantOn">Whether the player asked for it on.</param>
        /// <param name="requests">The player's waiting requests, changed by a queued or cancelled one.</param>
        public static PvpCombatChangeOutcome RequestFlag(
            bool windowOpen,
            bool flagIsOn,
            bool wantOn,
            PvpCombatRequests requests)
        {
            if (wantOn)
            {
                if (requests.IsFlagOffQueued)
                {
                    requests.IsFlagOffQueued = false;
                    return PvpCombatChangeOutcome.Cancelled;
                }

                return flagIsOn ? PvpCombatChangeOutcome.Unchanged : PvpCombatChangeOutcome.Applied;
            }

            if (!flagIsOn)
                return PvpCombatChangeOutcome.Unchanged;

            if (!windowOpen)
                return PvpCombatChangeOutcome.Applied;

            if (requests.IsFlagOffQueued)
                return PvpCombatChangeOutcome.Unchanged;

            requests.IsFlagOffQueued = true;
            return PvpCombatChangeOutcome.Queued;
        }

        /// <summary>
        /// Decides a request to show another identity (a disguise id, or empty for the player's own character), and
        /// records a waiting request in <paramref name="requests"/>. Asking for the identity already shown undoes a
        /// waiting change.
        /// </summary>
        /// <param name="windowOpen">Whether the player's window is open.</param>
        /// <param name="currentDisguiseId">The disguise shown now, or empty.</param>
        /// <param name="wantedDisguiseId">The disguise the player asked for, or empty for their own character.</param>
        /// <param name="requests">The player's waiting requests, changed by a queued or cancelled one.</param>
        public static PvpCombatChangeOutcome RequestIdentity(
            bool windowOpen,
            string currentDisguiseId,
            string wantedDisguiseId,
            PvpCombatRequests requests)
        {
            currentDisguiseId ??= string.Empty;
            wantedDisguiseId ??= string.Empty;

            if (currentDisguiseId == wantedDisguiseId)
            {
                if (!requests.IsIdentityChangeQueued)
                    return PvpCombatChangeOutcome.Unchanged;

                requests.IsIdentityChangeQueued = false;
                requests.QueuedDisguiseId = string.Empty;
                return PvpCombatChangeOutcome.Cancelled;
            }

            if (!windowOpen)
            {
                requests.IsIdentityChangeQueued = false;
                requests.QueuedDisguiseId = string.Empty;
                return PvpCombatChangeOutcome.Applied;
            }

            requests.IsIdentityChangeQueued = true;
            requests.QueuedDisguiseId = wantedDisguiseId;
            return PvpCombatChangeOutcome.Queued;
        }

        /// <summary>
        /// Whether waiting requests take effect now: once the window has closed. The flag goes off first, while the
        /// identity that asked is still the one shown, then the identity changes.
        /// </summary>
        public static bool ShouldApplyQueued(PvpCombatRequests requests, bool windowOpen)
        {
            return requests.HasAny && !windowOpen;
        }
    }
}

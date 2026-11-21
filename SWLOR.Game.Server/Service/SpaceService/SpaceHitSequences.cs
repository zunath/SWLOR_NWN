namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The arithmetic of "a target hit by N area abilities within a window suffers a status", in the ground's terms
    /// (Throwing Saturation Toss): each hit inside the window adds to the count, a hit after the window starts a new
    /// count, and reaching the required count triggers the status and clears the count.
    /// </summary>
    public static class SpaceHitSequences
    {
        /// <summary>The count after a hit at <paramref name="now"/>.</summary>
        public static int NextCount(SpaceHitSequenceState current, DateTime now, int windowSeconds)
        {
            return current != null && (now - current.LastHit).TotalSeconds <= windowSeconds
                ? current.Count + 1
                : 1;
        }

        /// <summary>Whether a count has reached the number of hits that triggers the status.</summary>
        public static bool IsTriggered(int count, int requiredCount) => requiredCount > 0 && count >= requiredCount;
    }
}

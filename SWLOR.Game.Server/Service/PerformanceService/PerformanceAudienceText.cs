using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>The wording of the audience lines in the performer's cue windows. Pure.</summary>
    public static class PerformanceAudienceText
    {
        public static string Venue(int rating)
        {
            return $"{PerformanceMath.ClampVenue(rating)} / {PerformanceMath.MaxVenueRating}";
        }

        /// <summary>"2 of 3 in range": the watchers following this performer, of every watcher in range.</summary>
        public static string Following(int followers, int watchersInRange)
        {
            return $"{followers} of {watchersInRange} in range";
        }

        public static string Inspiring(int followers, int watchersInRange)
        {
            if (followers > 0)
                return $"Inspiring {followers} {(followers == 1 ? "follower" : "followers")}";

            return watchersInRange > 0 ? "Nobody is following you yet" : "Practice: nobody in range";
        }

        public static string Hears(string songName)
        {
            return string.IsNullOrEmpty(songName) ? string.Empty : $"Audience hears: {songName}";
        }

        /// <summary>
        /// A musician's band line: who leads, which dancers are in range, and the bonuses that dancing and the troupe give.
        /// </summary>
        /// <param name="isLeader">The musician started first, so the band plays their song.</param>
        /// <param name="hasBandmates">Another musician plays in the same band.</param>
        /// <param name="leaderName">The band leader's name as the musician knows it.</param>
        /// <param name="dancerNames">The dancers within the musician's range.</param>
        /// <param name="harmonyPercent">The Harmony bonus while a dancer performs in range; 0 when it does not apply.</param>
        /// <param name="troupeCount">The performers, the musician included, who are in each other's range.</param>
        public static string Band(
            bool isLeader,
            bool hasBandmates,
            string leaderName,
            IReadOnlyList<string> dancerNames,
            int harmonyPercent,
            int troupeCount)
        {
            var text = !hasBandmates
                ? "You are playing on your own."
                : isLeader ? "You lead the band." : $"You play in {leaderName}'s band.";

            text += dancerNames.Count > 0
                ? $" Dancing in range: {string.Join(", ", dancerNames)}."
                : " No one is dancing in range.";

            var bonuses = new List<string>();
            if (harmonyPercent > 0)
                bonuses.Add($"Harmony +{harmonyPercent}%");

            var troupePercent = (int)Math.Round(PerformanceMath.TroupeBonus(troupeCount) * 100.0, MidpointRounding.AwayFromZero);
            if (troupeCount >= 2 && troupePercent > 0)
                bonuses.Add($"Troupe of {troupeCount} +{troupePercent}%");

            return bonuses.Count == 0 ? text : $"{text} {string.Join(", ", bonuses)}.";
        }
    }
}

using System.Collections.Generic;
using System.Globalization;

namespace SWLOR.Game.Server.Domain.Social
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

        /// <summary>
        /// What a watcher who follows only this performer leaves with, and how fast it banks at the performer's current quality
        /// and venue. A partner's riders and Harmony count as the last audience tick found them; the troupe bonus and a second
        /// performer the watcher also follows depend on the watcher, so they are left out.
        /// </summary>
        public static string AudienceGets(PerformerSnapshot performer)
        {
            var isDancer = performer.Kind == PerformanceKind.Dance;
            var inspired = PerformanceInspiredComposer.Compose(
                isDancer ? performer : null,
                isDancer ? null : performer,
                performer.Venue);

            var harmony = !isDancer && performer.HarmonyActive ? performer.BankingBonusPercent : 0;
            var rate = PerformanceMath.BankingMinutes(
                performer.Quality.GetBankingMultiplier(),
                performer.Venue,
                1,
                isDancer ? performer.BankingBonusPercent : 0,
                harmony);
            var cap = PerformanceMath.InspiredCapMinutes(performer.Venue, isDancer ? performer.CapBonusMinutes : 0);

            var banking = $"Banks {rate.ToString("0.#", CultureInfo.InvariantCulture)} min per minute watched " +
                          $"after {PerformanceMath.WarmupMinutes} min, up to {cap} min.";
            var debt = Math.Max(0, performer.DebtClearPercent);
            if (debt > 0)
                banking += $" Clears {debt}% XP debt per minute.";

            return $"Your audience gets {PerformanceWatchText.LeaveWith(inspired)}\n{banking}";
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

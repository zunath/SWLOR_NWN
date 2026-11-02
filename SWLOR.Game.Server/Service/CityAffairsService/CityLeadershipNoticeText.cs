using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The notices a citizen reads at login about their city's leadership: a step-down waiting for their answer and an
    /// Acting Mayor term they hold. City processing runs only at server start, so nothing in game announces these
    /// otherwise. Pure, so the wording can be tested.
    /// </summary>
    public static class CityLeadershipNoticeText
    {
        private const string Location = "Use the City Management terminal in City Hall";

        /// <summary>The notice for the player a mayor named interim holder.</summary>
        /// <param name="cityName">The city.</param>
        /// <param name="daysLeft">Whole days left to accept.</param>
        public static string BuildStepDownNotice(string cityName, int daysLeft)
        {
            return $"The mayor of {cityName} named you to hold the seat if they step down, with {DescribeDays(daysLeft)} left to answer. " +
                   $"{Location}, open the Succession tab, and accept or decline.";
        }

        /// <summary>The notice for the player who is Acting Mayor.</summary>
        public static string BuildActingMayorNotice(string cityName, DateTime endsUtc, DateTime nowUtc)
        {
            return $"You are the Acting Mayor of {cityName} for {DescribeDays(ElectionRules.GetDaysLeft(nowUtc, endsUtc))} more. " +
                   "You can decide City Affairs, change taxes, pay upkeep, and enact Edicts into empty slots.";
        }

        private static string DescribeDays(int days)
        {
            if (days <= 0)
                return "less than a day";

            return days == 1 ? "1 day" : $"{days} days";
        }
    }
}

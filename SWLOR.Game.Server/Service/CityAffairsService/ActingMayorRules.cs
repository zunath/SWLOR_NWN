using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// The Acting Mayor's term (Player City Revamp, "Acting mayor and stepping down"): who the mayor may name, how long the
    /// term lasts, and what ends it. Pure, so each rule can be tested.
    /// </summary>
    public static class ActingMayorRules
    {
        /// <summary>The longest term, in days.</summary>
        public const int MaxTermDays = 30;

        /// <summary>The term lengths the Manage City window offers, in days. The longest is <see cref="MaxTermDays"/>.</summary>
        public static readonly IReadOnlyList<int> TermChoicesDays = new[] { 3, 7, 14, 21, MaxTermDays };

        /// <summary>A term is at least 1 day and at most <see cref="MaxTermDays"/>.</summary>
        public static int ClampDays(int days)
        {
            return Math.Clamp(days, 1, MaxTermDays);
        }

        public static DateTime GetEndsUtc(DateTime nowUtc, int days)
        {
            return nowUtc.AddDays(ClampDays(days));
        }

        /// <summary>Whether a term is running: someone is named and its end time has not passed.</summary>
        public static bool IsActive(string actingMayorId, DateTime? endsUtc, DateTime nowUtc)
        {
            return !string.IsNullOrWhiteSpace(actingMayorId) && endsUtc.HasValue && nowUtc < endsUtc.Value;
        }

        /// <summary>
        /// Whether a named term is over: the 30 days passed. A record with no end time is over too, since a term always has one.
        /// </summary>
        public static bool IsElapsed(string actingMayorId, DateTime? endsUtc, DateTime nowUtc)
        {
            return !string.IsNullOrWhiteSpace(actingMayorId) && (!endsUtc.HasValue || nowUtc >= endsUtc.Value);
        }

        /// <summary>
        /// Whether an election ends the term: an election that seats a mayor does, whether it picks a new one or confirms
        /// the sitting one, and so does any change of who holds the seat. An election that ends with nobody chosen, such as
        /// a tie or no votes, leaves things as they were.
        /// </summary>
        /// <param name="electionHadOutrightWin">A Yes in a confidence vote, or the most votes.</param>
        /// <param name="seatChangedHands">A different player is now mayor.</param>
        public static bool ElectionEndsTerm(bool electionHadOutrightWin, bool seatChangedHands)
        {
            return electionHadOutrightWin || seatChangedHands;
        }

        /// <summary>
        /// Why the mayor may not name the player Acting Mayor, or an empty string when they may. Only the mayor names one,
        /// and only a citizen in good standing: a citizen of this city who owes no taxes.
        /// </summary>
        /// <param name="namerRole">The role of the player naming.</param>
        /// <param name="mayorId">The mayor.</param>
        /// <param name="targetId">The citizen to name.</param>
        /// <param name="targetIsCitizen">The target is a citizen of this city and not deleted.</param>
        /// <param name="targetOwesTaxes">The target owes taxes to the city.</param>
        public static string CheckNaming(CityRole namerRole, string mayorId, string targetId, bool targetIsCitizen, bool targetOwesTaxes)
        {
            if (namerRole != CityRole.Mayor)
                return "Only the mayor can name an Acting Mayor.";

            if (string.IsNullOrWhiteSpace(targetId))
                return "Pick a citizen to name Acting Mayor.";

            if (targetId == mayorId)
                return "The mayor cannot be their own Acting Mayor.";

            if (!targetIsCitizen)
                return "Only a citizen of this city can be Acting Mayor.";

            if (targetOwesTaxes)
                return "That citizen owes taxes, so is not in good standing.";

            return string.Empty;
        }

        /// <summary>Why the player may not end the term, or an empty string when they may.</summary>
        public static string CheckEnding(CityRole enderRole, bool termIsRunning)
        {
            if (enderRole != CityRole.Mayor)
                return "Only the mayor can end the Acting Mayor's term.";

            return termIsRunning ? string.Empty : "There is no Acting Mayor.";
        }

        /// <summary>The Gazette text for a newly named Acting Mayor. It names no one: the entry shows its official.</summary>
        public static string BuildNamedText(DateTime endsUtc)
        {
            return $"The mayor named an Acting Mayor, who holds office until {endsUtc:yyyy-MM-dd HH:mm} UTC, or until the mayor ends the term. " +
                   "Anything they do stays done.";
        }

        /// <summary>The Gazette text for the end of a term.</summary>
        public static string BuildEndedText(ActingMayorEndReason reason)
        {
            return $"The Acting Mayor's term has ended: {DescribeReason(reason)}.";
        }

        public static string DescribeReason(ActingMayorEndReason reason)
        {
            switch (reason)
            {
                case ActingMayorEndReason.MayorEnded:
                    return "the mayor ended it";
                case ActingMayorEndReason.TermElapsed:
                    return "its time ran out";
                case ActingMayorEndReason.LeftCity:
                    return "the Acting Mayor left the city";
                case ActingMayorEndReason.ElectionSeatedMayor:
                    return "an election seated a mayor";
                case ActingMayorEndReason.MayorSteppedDown:
                    return "the mayor stepped down and the interim holder took over";
                default:
                    return "the term is over";
            }
        }

        /// <summary>The line the Succession tab shows for a running term.</summary>
        public static string BuildStatusText(string actingMayorName, DateTime endsUtc, DateTime nowUtc)
        {
            return $"Acting Mayor: {actingMayorName}, until {endsUtc:yyyy-MM-dd HH:mm} UTC ({ElectionRules.GetDaysLeft(nowUtc, endsUtc)} days left).";
        }
    }
}

using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// The words a tournament puts in the Gazette and in front of players, with no engine calls. The Gazette is public, so
    /// it names duelists only by how strangers see them (an entrant's label) and never by a character name.
    /// </summary>
    public static class TournamentText
    {
        public static string BuildPostedText(long prize, long entryFee, DateTime startsUtc)
        {
            var fee = entryFee > 0 ? $"an entry fee of {TournamentRules.FormatCredits(entryFee)} credits" : "free entry";
            return $"A tournament has been posted at the Dueling Ring: a prize of {TournamentRules.FormatCredits(prize)} credits, {fee}, " +
                   $"and sign-ups close {FormatUtc(startsUtc)}.";
        }

        public static string BuildBeganText(int entrants)
        {
            return $"The tournament has begun with {entrants} entrants.";
        }

        public static string BuildChampionText(string championLabel, long prize)
        {
            var champion = Clean(championLabel, "Someone");
            return prize > 0
                ? $"{champion} won the tournament and the prize of {TournamentRules.FormatCredits(prize)} credits."
                : $"{champion} won the tournament.";
        }

        public static string BuildCancelledText(string reason)
        {
            var because = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" {reason.Trim()}";
            return $"The tournament was cancelled.{because} Entry fees were returned and the prize went back to the treasury.";
        }

        public static string BuildNoChampionText()
        {
            return "Neither finalist showed for the final, so the tournament has no champion. Entry fees were returned and the prize went back to the treasury.";
        }

        /// <summary>
        /// The Gazette line for a finished match, or an empty string for a match nobody played that left no news (a bye).
        /// </summary>
        public static string BuildMatchText(CityTournament tournament, TournamentMatch match)
        {
            var rounds = TournamentRules.GetRoundCount(tournament.Matches);
            var round = TournamentRules.GetRoundName(match.Round, rounds);
            var first = GetLabel(tournament, match.PlayerAId);
            var second = GetLabel(tournament, match.PlayerBId);

            if (string.IsNullOrEmpty(match.WinnerId))
            {
                return match.Result == TournamentMatchResult.BothForfeit
                    ? Capitalize($"{first} and {second} both failed to show for {round} of the tournament, so neither advances.")
                    : string.Empty;
            }

            var winner = GetLabel(tournament, match.WinnerId);
            var loser = match.WinnerId == match.PlayerAId ? second : first;

            return match.Result switch
            {
                TournamentMatchResult.Victory => Capitalize($"{winner} defeated {loser} in {round} of the tournament."),
                TournamentMatchResult.Forfeit => Capitalize($"{loser} forfeited to {winner} in {round} of the tournament."),
                TournamentMatchResult.Official => Capitalize(
                    $"The match between {first} and {second} in {round} of the tournament was interrupted {TournamentRules.InterruptionsBeforeOfficial} times, " +
                    $"so an official decided it in favor of {winner}."),
                _ => string.Empty
            };
        }

        /// <summary>The login notice that tells a player sign-ups are open.</summary>
        public static string BuildSignUpNotice(string cityName, string areaName, long prize, long entryFee, DateTime startsUtc, DateTime nowUtc)
        {
            var place = string.IsNullOrWhiteSpace(areaName) ? "its Dueling Ring" : $"its Dueling Ring in {areaName.Trim()}";
            var fee = entryFee > 0 ? $"Entry fee: {TournamentRules.FormatCredits(entryFee)} credits." : "Entry is free.";
            var remaining = startsUtc > nowUtc
                ? $"Sign-ups close in {Time.GetTimeLongIntervals(startsUtc - nowUtc, false)}."
                : "Sign-ups are closing.";

            return $"{Clean(cityName, "A city")} is holding a tournament at {place}. Prize: {TournamentRules.FormatCredits(prize)} credits. {fee} {remaining} " +
                   "Sign up at the ring.";
        }

        public static string GetLabel(CityTournament tournament, string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return "no one";

            var label = tournament.Entrants.FirstOrDefault(x => x.PlayerId == playerId)?.Label;
            return Clean(label, "Someone");
        }

        public static string FormatUtc(DateTime utc)
        {
            return utc.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) + " UTC";
        }

        private static string Clean(string text, string fallback)
        {
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        private static string Capitalize(string text)
        {
            return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DuelService;

namespace SWLOR.Game.Server.Service.TournamentService
{
    /// <summary>
    /// A city tournament's rules (Player City Revamp, "Conflict and PvP"), with no engine or database calls: who may post,
    /// sign up and begin, how the bracket is built and advances, how no-shows, draws and interruptions are handled, and who
    /// is barred from the ring. <see cref="Tournaments"/> feeds these the game's data and saves what they change. Money is
    /// settled in <see cref="TournamentSettlementRules"/>.
    /// </summary>
    public static class TournamentRules
    {
        /// <summary>How long a called duelist has to stand in the ring before forfeiting the match.</summary>
        public const double NoShowMinutes = 5.0;

        /// <summary>The interruption that hands a match to an official instead of replaying it.</summary>
        public const int InterruptionsBeforeOfficial = 3;

        public const int MinimumEntrants = 2;

        /// <summary>The furthest ahead a tournament can be scheduled, which also bounds how long its prize stays in escrow unsupervised.</summary>
        public const int MaxLeadDays = 14;

        /// <summary>The most a prize or fee can be: credits are whole numbers a character can hold.</summary>
        public const long MaxCredits = int.MaxValue;

        /// <summary>The prizes the ring offers a poster, in credits.</summary>
        public static readonly IReadOnlyList<long> PrizeChoices = new long[] { 10_000, 25_000, 50_000, 100_000, 250_000 };

        /// <summary>The entry fees the ring offers a poster, in credits. Free entry is allowed.</summary>
        public static readonly IReadOnlyList<long> FeeChoices = new long[] { 0, 500, 1_000, 2_500, 5_000 };

        /// <summary>How long from now the poster can set the start, in hours.</summary>
        public static readonly IReadOnlyList<int> LeadHourChoices = new[] { 1, 6, 24, 48, 72 };

        // Posting, signing up and beginning

        /// <summary>
        /// Why the tournament cannot be posted, or an empty string. The prize comes from the ordinary treasury only:
        /// bounties and prizes never use restricted funds.
        /// </summary>
        /// <param name="canHandleCityAffairs">Whether the poster is the mayor or an officer.</param>
        /// <param name="ringHasOpenTournament">Whether the ring already holds a tournament that has not ended.</param>
        /// <param name="ordinaryTreasury">What the city's ordinary treasury holds.</param>
        public static string CheckPost(
            bool canHandleCityAffairs,
            bool ringHasOpenTournament,
            long prize,
            long entryFee,
            DateTime startsUtc,
            DateTime nowUtc,
            long ordinaryTreasury)
        {
            if (!canHandleCityAffairs)
                return "Only the mayor or an officer can post a tournament.";
            if (ringHasOpenTournament)
                return "This ring already has a tournament. Wait for it to end or cancel it first.";
            if (prize < 0 || prize > MaxCredits)
                return "That prize is not allowed.";
            if (entryFee < 0 || entryFee > MaxCredits)
                return "That entry fee is not allowed.";
            if (startsUtc <= nowUtc)
                return "The tournament must start in the future.";
            if (startsUtc > nowUtc.AddDays(MaxLeadDays))
                return $"A tournament cannot start more than {MaxLeadDays} days ahead.";
            if (prize > ordinaryTreasury)
                return $"The treasury holds {FormatCredits(ordinaryTreasury)} credits that can be spent on a prize. Restricted funds cannot pay one.";

            return string.Empty;
        }

        /// <summary>Why the player cannot sign up, or an empty string.</summary>
        public static string CheckSignUp(CityTournament tournament, string playerId, long gold, DateTime nowUtc)
        {
            if (tournament == null || tournament.State != TournamentState.SignUp)
                return "This tournament is not taking sign-ups.";
            if (nowUtc >= tournament.StartsUtc)
                return "Sign-ups closed when the tournament was due to start.";
            if (string.IsNullOrWhiteSpace(playerId))
                return "Only a player can sign up.";
            if (IsBarred(tournament, playerId))
                return "You are barred from this tournament.";
            if (IsEntrant(tournament, playerId))
                return "You have already signed up.";
            if (gold < tournament.EntryFee)
                return $"The entry fee is {FormatCredits(tournament.EntryFee)} credits.";

            return string.Empty;
        }

        /// <summary>Why the tournament cannot begin yet, or an empty string.</summary>
        public static string CheckBegin(CityTournament tournament, DateTime nowUtc)
        {
            if (tournament == null || tournament.State != TournamentState.SignUp)
                return "This tournament has already begun.";
            if (nowUtc < tournament.StartsUtc)
                return $"The tournament is due to start in {Time.GetTimeLongIntervals(tournament.StartsUtc - nowUtc, false)}.";
            if (tournament.Entrants.Count < MinimumEntrants)
                return $"At least {MinimumEntrants} entrants are needed. Cancel the tournament to refund the fees.";

            return string.Empty;
        }

        /// <summary>Why no match can be called now, or an empty string.</summary>
        public static string CheckCall(CityTournament tournament)
        {
            if (tournament == null || tournament.State != TournamentState.Running)
                return "The tournament is not running.";
            if (GetCurrentMatch(tournament) != null)
                return "A match is already in progress.";
            if (GetNextReady(tournament.Matches) == null)
                return "No match is ready to be called.";

            return string.Empty;
        }

        public static bool IsEntrant(CityTournament tournament, string playerId)
        {
            return tournament.Entrants.Any(x => x.PlayerId == playerId);
        }

        public static TournamentEntrant GetEntrant(CityTournament tournament, string playerId)
        {
            return tournament.Entrants.FirstOrDefault(x => x.PlayerId == playerId);
        }

        /// <summary>
        /// The payment id of the sign-up a credit payment paid for, or null when no entrant carries it. A restart in the
        /// middle of signing up leaves the payment's mark in the character file; the sign-up counts as saved only when the
        /// entrant names the payment.
        /// </summary>
        public static string FindEntryPaymentId(CityTournament tournament, string markId)
        {
            if (tournament == null || string.IsNullOrWhiteSpace(markId))
                return null;

            return tournament.Entrants.Any(x => x.PaymentId == markId) ? markId : null;
        }

        // The bracket

        /// <summary>
        /// A single-elimination bracket for the entrants in the order given. When the count is not a power of two the
        /// first places in the second half of the first round stand empty, and those duelists advance without a fight.
        /// </summary>
        public static List<TournamentMatch> BuildBracket(IReadOnlyList<string> entrantIds)
        {
            var matches = new List<TournamentMatch>();
            var count = entrantIds.Count;
            if (count < MinimumEntrants)
                return matches;

            var size = 2;
            while (size < count)
                size *= 2;

            var half = size / 2;
            for (var slot = 0; slot < half; slot++)
            {
                var second = half + slot < count ? entrantIds[half + slot] : string.Empty;
                var match = new TournamentMatch
                {
                    Round = 1,
                    Slot = slot,
                    PlayerAId = entrantIds[slot],
                    PlayerBId = second,
                    State = TournamentMatchState.Pending
                };

                if (string.IsNullOrEmpty(second))
                    Finish(match, match.PlayerAId, TournamentMatchResult.Bye);

                matches.Add(match);
            }

            var matchesInRound = half;
            for (var round = 2; matchesInRound > 1; round++)
            {
                matchesInRound /= 2;
                for (var slot = 0; slot < matchesInRound; slot++)
                {
                    matches.Add(new TournamentMatch { Round = round, Slot = slot, State = TournamentMatchState.Pending });
                }
            }

            Refresh(matches);
            return matches;
        }

        public static int GetRoundCount(IReadOnlyCollection<TournamentMatch> matches)
        {
            return matches.Count == 0 ? 0 : matches.Max(x => x.Round);
        }

        public static TournamentMatch FindMatch(IEnumerable<TournamentMatch> matches, int round, int slot)
        {
            return matches.FirstOrDefault(x => x.Round == round && x.Slot == slot);
        }

        /// <summary>The final: the single match of the last round.</summary>
        public static TournamentMatch GetFinal(IReadOnlyCollection<TournamentMatch> matches)
        {
            var rounds = GetRoundCount(matches);
            return rounds == 0 ? null : matches.FirstOrDefault(x => x.Round == rounds);
        }

        /// <summary>
        /// Fills each waiting match with the winners of the two matches that feed it, and settles a match whose other place
        /// can never be filled: a lone duelist advances, and a match with nobody left has no winner. Run it after every
        /// change to the bracket.
        /// </summary>
        public static void Refresh(IReadOnlyCollection<TournamentMatch> matches)
        {
            var rounds = GetRoundCount(matches);
            for (var round = 2; round <= rounds; round++)
            {
                foreach (var match in matches.Where(x => x.Round == round && x.State == TournamentMatchState.Pending))
                {
                    var left = FindMatch(matches, round - 1, match.Slot * 2);
                    var right = FindMatch(matches, round - 1, match.Slot * 2 + 1);
                    var leftDone = left is { State: TournamentMatchState.Done };
                    var rightDone = right is { State: TournamentMatchState.Done };

                    match.PlayerAId = leftDone ? left.WinnerId : string.Empty;
                    match.PlayerBId = rightDone ? right.WinnerId : string.Empty;

                    if (!leftDone || !rightDone)
                        continue;

                    if (string.IsNullOrEmpty(match.PlayerAId) && string.IsNullOrEmpty(match.PlayerBId))
                        Finish(match, string.Empty, TournamentMatchResult.NoContest);
                    else if (string.IsNullOrEmpty(match.PlayerAId) || string.IsNullOrEmpty(match.PlayerBId))
                        Finish(match, string.IsNullOrEmpty(match.PlayerAId) ? match.PlayerBId : match.PlayerAId, TournamentMatchResult.Bye);
                }
            }
        }

        /// <summary>Whether both duelists are known and the match has not been called.</summary>
        public static bool IsReady(TournamentMatch match)
        {
            return match.State == TournamentMatchState.Pending &&
                   !string.IsNullOrEmpty(match.PlayerAId) &&
                   !string.IsNullOrEmpty(match.PlayerBId);
        }

        /// <summary>The next match to call: the earliest round, then the lowest slot.</summary>
        public static TournamentMatch GetNextReady(IEnumerable<TournamentMatch> matches)
        {
            return matches.Where(IsReady).OrderBy(x => x.Round).ThenBy(x => x.Slot).FirstOrDefault();
        }

        /// <summary>The match that has been called and not finished, or null. Only one match runs at a time.</summary>
        public static TournamentMatch GetCurrentMatch(CityTournament tournament)
        {
            return tournament.Matches.FirstOrDefault(x =>
                x.State is TournamentMatchState.Called or TournamentMatchState.Active or TournamentMatchState.AwaitingOfficial);
        }

        /// <summary>Whether the final has an end, with or without a champion.</summary>
        public static bool IsFinished(CityTournament tournament)
        {
            return GetFinal(tournament.Matches) is { State: TournamentMatchState.Done };
        }

        /// <summary>The winner of the final, or an empty string while it is open or when nobody won it.</summary>
        public static string GetChampionId(CityTournament tournament)
        {
            var final = GetFinal(tournament.Matches);
            return final is { State: TournamentMatchState.Done } ? final.WinnerId : string.Empty;
        }

        public static string GetRoundName(int round, int rounds)
        {
            var fromEnd = rounds - round;
            return fromEnd switch
            {
                0 => "the final",
                1 => "the semifinal",
                2 => "the quarterfinal",
                _ => $"round {round}"
            };
        }

        /// <summary>Puts the entrants in a random order for the bracket. <paramref name="nextBelow"/> returns a number from 0 up to, not including, its argument.</summary>
        public static List<string> Shuffle(IEnumerable<string> ids, Func<int, int> nextBelow)
        {
            var list = ids.ToList();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = nextBelow(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        // Running a match

        /// <summary>Calls a ready match: its duelists have <see cref="NoShowMinutes"/> minutes to stand in the ring.</summary>
        public static void Call(TournamentMatch match, DateTime nowUtc)
        {
            match.State = TournamentMatchState.Called;
            match.CalledUtc = nowUtc;
        }

        /// <summary>The duel of a called match began.</summary>
        public static void MarkActive(TournamentMatch match)
        {
            match.State = TournamentMatchState.Active;
        }

        /// <summary>
        /// Who has failed to show. After <see cref="NoShowMinutes"/> a called match with a duelist missing is lost by that
        /// duelist; with both missing both lose it. A match with both duelists present is never decided here: its duel
        /// starts instead.
        /// </summary>
        public static NoShowDecision EvaluateNoShow(TournamentMatch match, bool playerAPresent, bool playerBPresent, DateTime nowUtc)
        {
            if (match.State != TournamentMatchState.Called || !match.CalledUtc.HasValue)
                return NoShowDecision.None;
            if (nowUtc < match.CalledUtc.Value.AddMinutes(NoShowMinutes))
                return NoShowDecision.None;
            if (playerAPresent && playerBPresent)
                return NoShowDecision.None;
            if (!playerAPresent && !playerBPresent)
                return NoShowDecision.BothForfeit;

            return playerAPresent ? NoShowDecision.PlayerBForfeits : NoShowDecision.PlayerAForfeits;
        }

        /// <summary>Settles a called match for a no-show.</summary>
        public static void ApplyNoShow(TournamentMatch match, NoShowDecision decision)
        {
            switch (decision)
            {
                case NoShowDecision.PlayerAForfeits:
                    Finish(match, match.PlayerBId, TournamentMatchResult.Forfeit);
                    break;
                case NoShowDecision.PlayerBForfeits:
                    Finish(match, match.PlayerAId, TournamentMatchResult.Forfeit);
                    break;
                case NoShowDecision.BothForfeit:
                    Finish(match, string.Empty, TournamentMatchResult.BothForfeit);
                    break;
            }
        }

        /// <summary>
        /// Applies the result of the match's duel. The first duelist is the duel's challenger. A victory or a forfeit
        /// settles the match; a draw replays it, with a fresh five minutes.
        /// </summary>
        /// <returns>True when the match is finished.</returns>
        public static bool ApplyDuelOutcome(TournamentMatch match, DuelOutcome outcome, DateTime nowUtc)
        {
            if (outcome.Type == DuelOutcomeType.Draw || outcome.Winner == DuelSide.None)
            {
                Call(match, nowUtc);
                return false;
            }

            var winner = outcome.Winner == DuelSide.Challenger ? match.PlayerAId : match.PlayerBId;
            Finish(match, winner, outcome.Type == DuelOutcomeType.Forfeit ? TournamentMatchResult.Forfeit : TournamentMatchResult.Victory);
            return true;
        }

        /// <summary>
        /// Someone else joined the fight and voided the match's duel. The match is replayed, with a fresh five minutes,
        /// until it has been interrupted <see cref="InterruptionsBeforeOfficial"/> times; then an official decides it.
        /// </summary>
        /// <returns>The state the match is in now.</returns>
        public static TournamentMatchState RecordInterruption(TournamentMatch match, DateTime nowUtc)
        {
            if (match.State != TournamentMatchState.Active)
                return match.State;

            match.Interruptions++;
            if (match.Interruptions >= InterruptionsBeforeOfficial)
            {
                match.State = TournamentMatchState.AwaitingOfficial;
                return match.State;
            }

            Call(match, nowUtc);
            return match.State;
        }

        /// <summary>Whether an official can pick this duelist as the winner of an interrupted match.</summary>
        public static bool CanDecide(TournamentMatch match, string winnerId)
        {
            return match.State == TournamentMatchState.AwaitingOfficial &&
                   !string.IsNullOrEmpty(winnerId) &&
                   (winnerId == match.PlayerAId || winnerId == match.PlayerBId);
        }

        /// <summary>An official picks the winner of a match interrupted too often.</summary>
        /// <returns>False, changing nothing, when the pick is not allowed.</returns>
        public static bool Decide(TournamentMatch match, string winnerId, string officialPlayerId)
        {
            if (!CanDecide(match, winnerId))
                return false;

            Finish(match, winnerId, TournamentMatchResult.Official);
            match.DecidedByPlayerId = officialPlayerId ?? string.Empty;
            return true;
        }

        /// <summary>
        /// After a restart every duel is gone. A match in progress is replayed and a called match starts its five minutes
        /// again, since nobody could have shown while the server was down. A match waiting on an official still waits.
        /// </summary>
        /// <returns>How many matches were put back.</returns>
        public static int ReplayInProgress(CityTournament tournament, DateTime nowUtc)
        {
            var changed = 0;
            foreach (var match in tournament.Matches.Where(x => x.State is TournamentMatchState.Active or TournamentMatchState.Called))
            {
                Call(match, nowUtc);
                changed++;
            }

            return changed;
        }

        // Barring

        public static bool IsBarred(CityTournament tournament, string playerId)
        {
            return !string.IsNullOrEmpty(playerId) && tournament.BarredPlayerIds.Contains(playerId);
        }

        /// <summary>Bars a player from the ring until the tournament ends.</summary>
        /// <returns>False when the player was already barred or is not a player.</returns>
        public static bool Bar(CityTournament tournament, string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId) || IsBarred(tournament, playerId))
                return false;

            tournament.BarredPlayerIds.Add(playerId);
            return true;
        }

        /// <summary>Whether an interrupter should be barred: a player who is neither of the match's duelists.</summary>
        public static bool ShouldBar(TournamentMatch match, string interrupterPlayerId)
        {
            return !string.IsNullOrWhiteSpace(interrupterPlayerId) &&
                   interrupterPlayerId != match.PlayerAId &&
                   interrupterPlayerId != match.PlayerBId;
        }

        // Display

        public static string FormatCredits(long credits)
        {
            return credits.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static void Finish(TournamentMatch match, string winnerId, TournamentMatchResult result)
        {
            match.State = TournamentMatchState.Done;
            match.WinnerId = winnerId ?? string.Empty;
            match.Result = result;
        }
    }
}

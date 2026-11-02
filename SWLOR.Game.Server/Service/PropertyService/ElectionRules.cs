using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// The rules of a city's mayoral election (Player City Revamp, "Election changes"): the calendar, who may stand and
    /// vote, and what each result does. Pure, so every row of the Plan's outcome tables can be tested.
    /// </summary>
    public static class ElectionRules
    {
        /// <summary>Days candidates can register, edit their statement, or withdraw.</summary>
        public const int RegistrationDays = 7;

        /// <summary>Days citizens vote once the ballot is fixed.</summary>
        public const int VotingDays = 7;

        /// <summary>Quiet days between the end of an election and the next regular election opening.</summary>
        public const int QuietDays = 14;

        /// <summary>The longest candidate statement, matching the HoloNet message limit.</summary>
        public const int StatementMaxLength = 600;

        /// <summary>How long after a result posts a citizen who has not read it still sees it at login.</summary>
        public const int ResultNoticeDays = 28;

        #region Calendar

        public static DateTime GetRegistrationCloseUtc(DateTime registrationOpenedUtc)
        {
            return registrationOpenedUtc.AddDays(RegistrationDays);
        }

        public static DateTime GetVotingCloseUtc(DateTime registrationClosesUtc)
        {
            return registrationClosesUtc.AddDays(VotingDays);
        }

        /// <summary>
        /// When an election ended, for scheduling what follows: registration closing when nobody ran, otherwise voting
        /// closing. Read from the dates stored on the election, never from today's constants.
        /// </summary>
        public static DateTime GetEndUtc(DateTime registrationClosesUtc, DateTime votingClosesUtc, bool voteWasHeld)
        {
            return voteWasHeld ? votingClosesUtc : registrationClosesUtc;
        }

        /// <summary>When the next regular election opens: the quiet period after the previous election ended.</summary>
        public static DateTime GetNextRegularElectionUtc(DateTime endedUtc)
        {
            return endedUtc.AddDays(QuietDays);
        }

        /// <summary>
        /// The step an election is due for. Dates come from the election record, so an election that opened under an
        /// older calendar closes when it was told it would.
        /// </summary>
        public static ElectionStageAction GetDueAction(
            ElectionStageType stage,
            DateTime nowUtc,
            DateTime registrationClosesUtc,
            DateTime votingClosesUtc)
        {
            if (stage == ElectionStageType.Registration && nowUtc >= registrationClosesUtc)
                return ElectionStageAction.CloseRegistration;

            if (stage == ElectionStageType.Voting && nowUtc >= votingClosesUtc)
                return ElectionStageAction.CloseVoting;

            return ElectionStageAction.None;
        }

        public static ElectionRegistrationResult GetRegistrationResult(int candidateCount)
        {
            if (candidateCount <= 0)
                return ElectionRegistrationResult.NoCandidates;

            return candidateCount == 1
                ? ElectionRegistrationResult.ConfidenceVote
                : ElectionRegistrationResult.ContestedVote;
        }

        /// <summary>Candidates register, edit their statement, or withdraw only while registration is open.</summary>
        public static bool IsRegistrationOpen(ElectionStageType stage, DateTime nowUtc, DateTime registrationClosesUtc)
        {
            return stage == ElectionStageType.Registration && nowUtc < registrationClosesUtc;
        }

        public static bool IsVotingOpen(ElectionStageType stage, DateTime nowUtc, DateTime votingClosesUtc)
        {
            return stage == ElectionStageType.Voting && nowUtc < votingClosesUtc;
        }

        /// <summary>Whole days left until a deadline, rounded up, never below zero.</summary>
        public static int GetDaysLeft(DateTime nowUtc, DateTime closesUtc)
        {
            var remaining = closesUtc - nowUtc;
            return remaining <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(remaining.TotalDays);
        }

        public static bool IsSnap(ElectionKind kind)
        {
            return kind == ElectionKind.SnapConfidence || kind == ElectionKind.SnapStepDown;
        }

        #endregion

        #region Who may take part

        /// <summary>A character who is a citizen of the city and has not been deleted.</summary>
        public static bool IsCitizenOf(string citizenPropertyId, string cityId, bool isDeleted)
        {
            return !isDeleted &&
                   !string.IsNullOrWhiteSpace(cityId) &&
                   citizenPropertyId == cityId;
        }

        /// <summary>
        /// A character may vote if it is a citizen and has been one since registration opened or earlier. Rejoining
        /// resets the citizen-since time, so a character that left and came back cannot vote in that election.
        /// </summary>
        public static bool IsEligibleVoter(
            string citizenPropertyId,
            string cityId,
            bool isDeleted,
            DateTime? citizenSinceUtc,
            DateTime registrationOpenedUtc)
        {
            return IsCitizenOf(citizenPropertyId, cityId, isDeleted) &&
                   citizenSinceUtc.HasValue &&
                   citizenSinceUtc.Value <= registrationOpenedUtc;
        }

        /// <summary>Trims a candidate statement and cuts it to <see cref="StatementMaxLength"/> characters.</summary>
        public static string NormalizeStatement(string statement)
        {
            if (string.IsNullOrWhiteSpace(statement))
                return string.Empty;

            var trimmed = statement.Trim();
            return trimmed.Length <= StatementMaxLength
                ? trimmed
                : trimmed.Substring(0, StatementMaxLength).TrimEnd();
        }

        #endregion

        #region Results

        /// <summary>
        /// Registration closed with nobody on the ballot. No vote is held and the incumbent stays; in a step-down
        /// election the incumbent is the interim holder, who becomes mayor. A recall that ends this way is empty.
        /// </summary>
        public static ElectionOutcome DecideNoCandidates(ElectionKind kind, string incumbentPlayerId)
        {
            return new ElectionOutcome(kind, ElectionOutcomeType.NoCandidates, incumbentPlayerId, 0, 0);
        }

        /// <summary>
        /// Voting closed. Decides the result from the candidates still on the ballot and the votes that count.
        /// </summary>
        /// <param name="kind">The kind of election.</param>
        /// <param name="isConfidenceVote">True when the ballot was a Yes or No vote on a lone candidate.</param>
        /// <param name="incumbentPlayerId">The sitting mayor. In a step-down election, the interim holder.</param>
        /// <param name="candidatePlayerIds">The candidates still on the ballot. Ones who left are not included.</param>
        /// <param name="votes">The votes that count. Votes for anyone not on the ballot are ignored.</param>
        public static ElectionOutcome DecideVoting(
            ElectionKind kind,
            bool isConfidenceVote,
            string incumbentPlayerId,
            IReadOnlyCollection<string> candidatePlayerIds,
            IEnumerable<ElectionVoter> votes)
        {
            var candidates = (candidatePlayerIds ?? Array.Empty<string>()).Distinct().ToList();
            var ballotVotes = (votes ?? Enumerable.Empty<ElectionVoter>()).ToList();

            // Every candidate left the ballot: the election ends as if nobody had voted.
            if (candidates.Count == 0)
                return new ElectionOutcome(kind, ElectionOutcomeType.NoVotes, incumbentPlayerId, 0, 0);

            return isConfidenceVote && candidates.Count == 1
                ? DecideConfidenceVote(kind, incumbentPlayerId, candidates[0], ballotVotes)
                : DecideContestedVote(kind, incumbentPlayerId, candidates, ballotVotes);
        }

        private static ElectionOutcome DecideConfidenceVote(
            ElectionKind kind,
            string incumbentPlayerId,
            string candidateId,
            IReadOnlyCollection<ElectionVoter> votes)
        {
            var yes = votes.Count(v => v.CandidatePlayerId == candidateId && !v.IsNo);
            var no = votes.Count(v => v.CandidatePlayerId == candidateId && v.IsNo);
            var isIncumbent = candidateId == incumbentPlayerId;
            var total = yes + no;

            if (total == 0)
                return new ElectionOutcome(kind, ElectionOutcomeType.NoVotes, incumbentPlayerId, 1, 0);

            if (yes > no)
            {
                return new ElectionOutcome(
                    kind,
                    isIncumbent ? ElectionOutcomeType.IncumbentConfirmed : ElectionOutcomeType.NewMayor,
                    candidateId,
                    1,
                    total);
            }

            if (no > yes)
            {
                return new ElectionOutcome(
                    kind,
                    isIncumbent ? ElectionOutcomeType.NoConfidence : ElectionOutcomeType.ChallengerRejected,
                    incumbentPlayerId,
                    1,
                    total);
            }

            return new ElectionOutcome(kind, ElectionOutcomeType.ConfidenceTie, incumbentPlayerId, 1, total);
        }

        private static ElectionOutcome DecideContestedVote(
            ElectionKind kind,
            string incumbentPlayerId,
            IReadOnlyCollection<string> candidates,
            IReadOnlyCollection<ElectionVoter> votes)
        {
            var counts = candidates.ToDictionary(candidate => candidate, _ => 0);
            foreach (var vote in votes)
            {
                if (counts.ContainsKey(vote.CandidatePlayerId))
                    counts[vote.CandidatePlayerId]++;
            }

            var total = counts.Values.Sum();
            if (total == 0)
                return new ElectionOutcome(kind, ElectionOutcomeType.NoVotes, incumbentPlayerId, candidates.Count, 0);

            var most = counts.Values.Max();
            var leaders = counts.Where(pair => pair.Value == most).Select(pair => pair.Key).ToList();

            // Nobody won outright, so the incumbent stays even if they were one of the tied candidates.
            if (leaders.Count > 1)
                return new ElectionOutcome(kind, ElectionOutcomeType.Tie, incumbentPlayerId, candidates.Count, total);

            var winner = leaders[0];
            return new ElectionOutcome(
                kind,
                winner == incumbentPlayerId ? ElectionOutcomeType.IncumbentConfirmed : ElectionOutcomeType.NewMayor,
                winner,
                candidates.Count,
                total);
        }

        #endregion
    }
}

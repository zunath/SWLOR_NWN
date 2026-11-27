namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// What an election's result does to the city, decided by <see cref="ElectionRules"/> from the Plan's outcome
    /// tables. The service applies it: it never decides anything itself.
    /// </summary>
    public sealed class ElectionOutcome
    {
        public ElectionOutcome(
            ElectionKind kind,
            ElectionOutcomeType type,
            string winnerPlayerId,
            int candidateCount,
            int votesCast)
        {
            Kind = kind;
            Type = type;
            WinnerPlayerId = winnerPlayerId ?? string.Empty;
            CandidateCount = candidateCount;
            VotesCast = votesCast;
        }

        public ElectionKind Kind { get; }

        public ElectionOutcomeType Type { get; }

        /// <summary>
        /// The mayor after the election: the winner of an outright win, otherwise the incumbent who stays. In a
        /// step-down election the incumbent is the interim holder, who becomes mayor wherever the incumbent stays.
        /// </summary>
        public string WinnerPlayerId { get; }

        /// <summary>The candidates left on the ballot.</summary>
        public int CandidateCount { get; }

        /// <summary>The votes that counted: Yes and No in a confidence vote, otherwise votes for a candidate on the ballot.</summary>
        public int VotesCast { get; }

        /// <summary>
        /// An outright win: a Yes in a confidence vote, or the most votes. Only an outright win enacts a platform and
        /// ends a No confidence pause; ties, No results and elections without votes leave things as they were.
        /// </summary>
        public bool HasMandate => Type == ElectionOutcomeType.NewMayor || Type == ElectionOutcomeType.IncumbentConfirmed;

        /// <summary>True when citizens voted No on the incumbent. The city is then under No confidence.</summary>
        public bool SetsNoConfidence => Type == ElectionOutcomeType.NoConfidence;

        /// <summary>
        /// True when a snap confidence election opens at once. Only a regular or recall election opens one; a snap
        /// election never opens another.
        /// </summary>
        public bool OpensSnapElection =>
            SetsNoConfidence && (Kind == ElectionKind.Regular || Kind == ElectionKind.Recall);

        /// <summary>
        /// True when the election ended with at least one candidate and one vote, whatever the result, because
        /// citizens took part. That ends a Neglect pause.
        /// </summary>
        public bool ClearsNeglect => CandidateCount >= 1 && VotesCast >= 1;

        /// <summary>No candidates registered, nobody voted, or every candidate left the ballot.</summary>
        public bool IsEmpty => Type == ElectionOutcomeType.NoCandidates || Type == ElectionOutcomeType.NoVotes;

        /// <summary>A recall that ended with no candidates, no votes, or every candidate gone.</summary>
        public bool IsEmptyRecall => Kind == ElectionKind.Recall && IsEmpty;

        /// <summary>False only when registration closed with no candidates, so no vote was held.</summary>
        public bool VoteWasHeld => Type != ElectionOutcomeType.NoCandidates;
    }
}

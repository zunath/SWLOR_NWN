namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// How an election ended. Values are stored on <see cref="Entity.ElectionResultRecord"/>, so they never change.
    /// </summary>
    public enum ElectionOutcomeType
    {
        Invalid = 0,

        /// <summary>Registration closed with no candidates. There is no vote and the incumbent stays.</summary>
        NoCandidates = 1,

        /// <summary>Voting closed with no votes, or every candidate left the ballot. The incumbent stays.</summary>
        NoVotes = 2,

        /// <summary>An outright win by a candidate other than the incumbent: Yes won a confidence vote, or they had the most votes.</summary>
        NewMayor = 3,

        /// <summary>An outright win by the incumbent: Yes won a confidence vote on them, or they had the most votes.</summary>
        IncumbentConfirmed = 4,

        /// <summary>No won a confidence vote on a challenger. The incumbent stays.</summary>
        ChallengerRejected = 5,

        /// <summary>No won a confidence vote on the incumbent.</summary>
        NoConfidence = 6,

        /// <summary>Yes and No tied in a confidence vote. The incumbent stays.</summary>
        ConfidenceTie = 7,

        /// <summary>Two or more candidates tied for the most votes. The incumbent stays, even if one of them.</summary>
        Tie = 8,
    }
}

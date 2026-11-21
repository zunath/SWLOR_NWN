namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>What happens when an election's registration closes.</summary>
    public enum ElectionRegistrationResult
    {
        /// <summary>Nobody registered. There is no vote.</summary>
        NoCandidates = 0,

        /// <summary>One candidate registered. Citizens vote Yes or No on them.</summary>
        ConfidenceVote = 1,

        /// <summary>Two or more candidates registered. Citizens vote for one of them.</summary>
        ContestedVote = 2,
    }
}

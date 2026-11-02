namespace SWLOR.Game.Server.Entity
{
    /// <summary>One CD key's vote in an election.</summary>
    public class ElectionVoter
    {
        public ElectionVoter()
        {
            VoterPlayerId = string.Empty;
            CandidatePlayerId = string.Empty;
        }

        /// <summary>The character that cast the vote.</summary>
        public string VoterPlayerId { get; set; }

        /// <summary>The candidate voted for. In a confidence vote, the one candidate the vote is about.</summary>
        public string CandidatePlayerId { get; set; }

        /// <summary>True for a No vote in a confidence vote. Ignored in a contested vote.</summary>
        public bool IsNo { get; set; }
    }
}

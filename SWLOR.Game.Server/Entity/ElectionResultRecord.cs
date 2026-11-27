using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The result of a city's latest election, kept on the city so citizens can read it at their next login.
    /// Its <see cref="Id"/> is the id of the election it ended, which lets the next boot tell that an election
    /// was already concluded when the server stopped before the election record was removed.
    /// </summary>
    public class ElectionResultRecord
    {
        public ElectionResultRecord()
        {
            Id = string.Empty;
            WinnerPlayerId = string.Empty;
        }

        /// <summary>The id of the election this is the result of.</summary>
        public string Id { get; set; }

        public ElectionKind Kind { get; set; }

        public ElectionOutcomeType Outcome { get; set; }

        /// <summary>
        /// The mayor after the election. For an outright win, the winner; otherwise the incumbent who stayed.
        /// </summary>
        public string WinnerPlayerId { get; set; }

        /// <summary>The candidates left on the ballot at the end.</summary>
        public int CandidateCount { get; set; }

        /// <summary>The votes that counted.</summary>
        public int VotesCast { get; set; }

        /// <summary>True when this result opened a snap confidence election at once.</summary>
        public bool OpenedSnapElection { get; set; }

        /// <summary>True when this result was an empty recall that opened the second recall at once.</summary>
        public bool OpenedSecondRecall { get; set; }

        /// <summary>When the next regular election opens. Null when a snap election opened instead.</summary>
        public DateTime? NextElectionUtc { get; set; }

        /// <summary>When the result posted.</summary>
        public DateTime PostedUtc { get; set; }
    }
}

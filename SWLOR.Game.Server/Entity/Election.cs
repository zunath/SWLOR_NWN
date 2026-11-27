using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A city's open mayoral election. It exists from the day registration opens until its result posts.
    /// Stage dates are stored when the election opens, so a later change to the calendar never moves the cutoff
    /// of an election already running.
    /// </summary>
    public class Election: EntityBase
    {
        public Election()
        {
            Kind = ElectionKind.Regular;
            CandidatePlayerIds = new List<string>();
            VoterSelections = new Dictionary<string, ElectionVoter>();
            Platforms = new Dictionary<string, ElectionPlatform>();
        }

        [Indexed]
        public string PropertyId { get; set; }

        public ElectionStageType Stage { get; set; }

        /// <summary>Why the election opened. Records saved before elections had a kind load as Regular.</summary>
        public ElectionKind Kind { get; set; }

        /// <summary>When registration opened. Voters must have been citizens since this time or earlier.</summary>
        public DateTime RegistrationOpenedUtc { get; set; }

        /// <summary>When registration closes and the ballot is fixed.</summary>
        public DateTime RegistrationClosesUtc { get; set; }

        /// <summary>When voting closes and the votes are counted.</summary>
        public DateTime VotingClosesUtc { get; set; }

        /// <summary>
        /// True when exactly one candidate registered, so citizens vote Yes or No on them instead of choosing between
        /// candidates. Set when registration closes.
        /// </summary>
        public bool IsConfidenceVote { get; set; }

        public List<string> CandidatePlayerIds { get; set; }

        /// <summary>
        /// What each candidate stands for, by player id. Registering creates the entry, even for a platform with no Edicts.
        /// A candidate of an election that was already open when platforms arrived has none until they edit their platform.
        /// </summary>
        public Dictionary<string, ElectionPlatform> Platforms { get; set; }

        /// <summary>One vote per CD key, by CD key.</summary>
        public Dictionary<string, ElectionVoter> VoterSelections { get; set; }
    }
}

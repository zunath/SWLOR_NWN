using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Data.Entity
{
    public class Election: EntityBase
    {
        public Election()
        {
            CandidatePlayerIds = new List<string>();
            VoterSelections = new Dictionary<string, ElectionVoter>();
        }

        [Indexed]
        public string PropertyId { get; set; }

        public ElectionStageType Stage { get; set; }

        public List<string> CandidatePlayerIds { get; set; }

        public Dictionary<string, ElectionVoter> VoterSelections { get; set; }

    }

    public class ElectionVoter
    {
        public ElectionVoter()
        {
            VoterPlayerId = string.Empty;
            CandidatePlayerId = string.Empty;
        }

        public string VoterPlayerId { get; set; }
        public string CandidatePlayerId { get; set; }
    }
}

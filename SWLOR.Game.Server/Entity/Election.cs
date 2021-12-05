using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public class Election: EntityBase
    {
        public Election()
        {
            CandidatePlayerIds = new List<string>();
            VoterSelections = new Dictionary<string, string>();
        }

        [Indexed]
        public string PropertyId { get; set; }

        public ElectionStageType Stage { get; set; }

        public List<string> CandidatePlayerIds { get; set; }

        public Dictionary<string, string> VoterSelections { get; set; }

    }
}

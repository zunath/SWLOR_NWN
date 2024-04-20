using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class MutationDetail
    {
        public BeastType Type { get; private set; }
        public List<IMutationRequirement> Requirements { get; set; }
        public int Weight { get; set; }

        public MutationDetail(BeastType type)
        {
            Type = type;
            Requirements = new List<IMutationRequirement>();
            Weight = 10;
        }
    }
}

using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Models
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

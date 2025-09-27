using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;

namespace SWLOR.Shared.Domain.Associate.ValueObjects
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

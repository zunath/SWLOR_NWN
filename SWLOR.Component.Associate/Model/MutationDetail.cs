using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Enums;

namespace SWLOR.Component.Associate.Model
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

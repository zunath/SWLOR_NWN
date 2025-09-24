using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Crafting.Contracts;

namespace SWLOR.Shared.Domain.Crafting.ValueObjects
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

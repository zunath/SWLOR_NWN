using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Beasts.ValueObjects;

namespace SWLOR.Component.Associate.Contracts
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}

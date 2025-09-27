using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Component.Associate.Contracts
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}

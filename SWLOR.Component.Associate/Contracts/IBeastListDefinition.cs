using SWLOR.Component.Associate.Enums;
using SWLOR.Component.Associate.Model;

namespace SWLOR.Component.Associate.Contracts
{
    public interface IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build();
    }
}

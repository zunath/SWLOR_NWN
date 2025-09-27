using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;

namespace SWLOR.Component.Perk.Contracts
{
    public interface IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder);
    }
}

using SWLOR.Component.Perk.Enums;
using SWLOR.Component.Perk.Model;

namespace SWLOR.Component.Perk.Contracts
{
    public interface IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks();
    }
}

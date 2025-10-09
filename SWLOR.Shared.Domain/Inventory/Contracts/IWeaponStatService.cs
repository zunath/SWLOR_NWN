using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Shared.Domain.Inventory.Contracts
{
    public interface IWeaponStatService
    {
        WeaponStat LoadWeaponStat(uint item);
    }
}

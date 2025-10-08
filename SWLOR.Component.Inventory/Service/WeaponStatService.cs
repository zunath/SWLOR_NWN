using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Service
{
    internal class WeaponStatService: IWeaponStatService
    {
        public WeaponStat LoadWeaponStat(uint weapon)
        {
            var stat = new WeaponStat();
            if (!GetIsObjectValid(weapon))
                return stat;

            stat.Item = weapon;
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                var value = GetItemPropertyCostTableValue(ip);
                if (type == ItemPropertyType.DMG)
                {
                    stat.DMG = value;
                }
                else if (type == ItemPropertyType.Delay)
                {
                    stat.Delay = value * 10;
                }
            }

            return stat;
        }
    }
}

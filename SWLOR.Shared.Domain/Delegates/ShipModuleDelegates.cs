using SWLOR.Shared.Domain.Model;

namespace SWLOR.Shared.Domain.Delegates
{
    public delegate void ShipModuleEquippedDelegate(ShipStatus shipStatus, int moduleBonus);
    public delegate void ShipModuleUnequippedDelegate(ShipStatus shipStatus, int moduleBonus);
    public delegate float ShipModuleCalculateRecastDelegate(uint creature, ShipStatus shipStatus, int moduleBonus);
    public delegate int ShipModuleCalculateCapacitorDelegate(uint creature, ShipStatus shipStatus, int moduleBonus);
    public delegate void ShipModuleActivatedDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);
    public delegate string ShipModuleValidationDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);
    public delegate float ShipModuleCalculateMaxDistanceDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);
}

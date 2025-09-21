using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

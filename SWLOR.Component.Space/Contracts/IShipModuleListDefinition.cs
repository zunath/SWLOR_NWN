using SWLOR.Shared.Domain.Space.ValueObjects;

namespace SWLOR.Component.Space.Contracts
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

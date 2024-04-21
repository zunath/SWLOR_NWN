namespace SWLOR.Core.Service.SpaceService
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

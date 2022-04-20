using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

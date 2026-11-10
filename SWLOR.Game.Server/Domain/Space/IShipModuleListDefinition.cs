using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Space
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

using System.Collections.Generic;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipModuleListDefinition
    {
        public Dictionary<string, ShipModuleDetail> BuildShipModules();
    }
}

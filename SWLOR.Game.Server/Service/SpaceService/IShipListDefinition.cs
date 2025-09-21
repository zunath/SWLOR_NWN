using System.Collections.Generic;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips();
    }
}

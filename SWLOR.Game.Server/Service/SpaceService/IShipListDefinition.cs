using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipListDefinition
    {
        public Dictionary<ShipType, ShipDetail> BuildShips();
    }
}

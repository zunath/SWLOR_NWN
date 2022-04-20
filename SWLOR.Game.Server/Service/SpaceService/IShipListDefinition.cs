using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips();
    }
}

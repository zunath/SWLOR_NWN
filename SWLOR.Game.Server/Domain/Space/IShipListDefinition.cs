using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Space
{
    public interface IShipListDefinition
    {
        public Dictionary<string, ShipDetail> BuildShips();
    }
}

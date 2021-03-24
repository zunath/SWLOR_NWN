using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface IShipEnemyListDefinition
    {
        public Dictionary<string, ShipEnemyDetail> BuildShipEnemies();
    }
}

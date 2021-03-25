using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipEnemyDetail
    {
        public string ShipItemTag { get; set; }

        public List<string> HighPoweredModules { get; set; }
        public List<string> LowPowerModules { get; set; }

        public ShipEnemyDetail()
        {
            HighPoweredModules = new List<string>();
            LowPowerModules = new List<string>();
        }
    }
}

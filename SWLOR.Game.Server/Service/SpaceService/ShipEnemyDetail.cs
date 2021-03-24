using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipEnemyDetail
    {
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }

        public int Accuracy { get; set; }
        public int Evasion { get; set; }

        public int EMDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int ThermalDefense { get; set; }

        public List<string> HighPoweredModules { get; set; }
        public List<string> LowPowerModules { get; set; }

        public ShipEnemyDetail()
        {
            HighPoweredModules = new List<string>();
            LowPowerModules = new List<string>();
        }
    }
}

using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Space
{
    public class SpaceObjectDetail
    {
        public string ShipItemTag { get; set; }

        public List<string> HighPoweredModules { get; set; }
        public List<string> LowPowerModules { get; set; }
        public List<string> ConfigurationModules { get; set; }

        public SpaceObjectDetail()
        {
            HighPoweredModules = new List<string>();
            LowPowerModules = new List<string>();
            ConfigurationModules = new List<string>();
        }
    }
}

using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public class PropertyInstance
    {
        public PropertyInstance(uint area)
        {
            Area = area;
            Players = new List<uint>();
        }

        public uint Area { get; set; }
        public List<uint> Players { get; set; }
    }
}

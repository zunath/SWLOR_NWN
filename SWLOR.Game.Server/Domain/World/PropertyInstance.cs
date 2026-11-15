using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.World
{
    public class PropertyInstance
    {
        public PropertyInstance(uint area, PropertyLayoutType layoutType)
        {
            Area = area;
            Players = new List<uint>();
            LayoutType = layoutType;
        }

        public uint Area { get; set; }
        public List<uint> Players { get; set; }
        public PropertyLayoutType LayoutType { get; set; }
    }
}

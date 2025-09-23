using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model
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

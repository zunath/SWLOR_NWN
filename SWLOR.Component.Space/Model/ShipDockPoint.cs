using SWLOR.NWN.API.Engine;

namespace SWLOR.Component.Space.Model
{
    public class ShipDockPoint
    {
        public string Name { get; set; }
        public string PropertyId { get; set; }
        public Location Location { get; set; }
        public bool IsNPC { get; set; }
    }
}

using SWLOR.NWN.API.Engine;

namespace SWLOR.Shared.Domain.Space.ValueObjects
{
    public class ShipDockPoint
    {
        public string Name { get; set; }
        public string PropertyId { get; set; }
        public Location Location { get; set; }
        public bool IsNPC { get; set; }
    }
}

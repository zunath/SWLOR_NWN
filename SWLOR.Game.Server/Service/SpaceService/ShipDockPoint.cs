using SWLOR.Game.Server.Core;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipDockPoint
    {
        public string Name { get; set; }
        public string PropertyId { get; set; }
        public Location Location { get; set; }
        public bool IsNPC { get; set; }
    }
}

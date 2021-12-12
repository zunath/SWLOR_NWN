using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipDockPoint
    {
        public string Name { get; set; }
        public Location Location { get; set; }
        public bool IsNPC { get; set; }
    }
}

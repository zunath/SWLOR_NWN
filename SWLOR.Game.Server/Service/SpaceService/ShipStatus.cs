using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class ShipStatus
    {
        public uint Creature { get; set; }
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int Evasion { get; set; }
        public int Accuracy { get; set; }

        public ShipStatus()
        {
            Creature = OBJECT_INVALID;
        }
    }
}

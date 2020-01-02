using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnPerkRefunded
    {
        public NWPlayer Player { get; set; }
        public PerkType PerkType { get; set; }

        public OnPerkRefunded(NWPlayer player, PerkType perkType)
        {
            Player = player;
            PerkType = perkType;
        }
    }
}

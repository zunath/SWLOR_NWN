using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnPerkUpgraded
    {
        public NWPlayer Player { get; set; }
        public PerkType PerkType { get; set; }

        public OnPerkUpgraded(NWPlayer player, PerkType perkType)
        {
            Player = player;
            PerkType = perkType;
        }
    }
}

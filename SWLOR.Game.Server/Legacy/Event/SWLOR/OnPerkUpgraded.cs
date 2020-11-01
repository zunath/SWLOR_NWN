using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnPerkUpgraded
    {
        public NWPlayer Player { get; set; }
        public int PerkID { get; set; }

        public OnPerkUpgraded(NWPlayer player, int perkID)
        {
            Player = player;
            PerkID = perkID;
        }
    }
}

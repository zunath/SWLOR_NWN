using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
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

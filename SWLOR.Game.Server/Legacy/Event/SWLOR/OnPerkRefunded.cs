using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnPerkRefunded
    {
        public NWPlayer Player { get; set; }
        public int PerkID { get; set; }

        public OnPerkRefunded(NWPlayer player, int perkID)
        {
            Player = player;
            PerkID = perkID;
        }
    }
}

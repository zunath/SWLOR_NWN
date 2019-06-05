using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
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

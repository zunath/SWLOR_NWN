using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class PerkUpgradedMessage
    {
        public NWPlayer Player { get; set; }
        public int PerkID { get; set; }

        public PerkUpgradedMessage(NWPlayer player, int perkID)
        {
            Player = player;
            PerkID = perkID;
        }
    }
}

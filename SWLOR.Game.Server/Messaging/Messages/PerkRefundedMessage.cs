using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class PerkRefundedMessage
    {
        public NWPlayer Player { get; set; }
        public int PerkID { get; set; }

        public PerkRefundedMessage(NWPlayer player, int perkID)
        {
            Player = player;
            PerkID = perkID;
        }
    }
}

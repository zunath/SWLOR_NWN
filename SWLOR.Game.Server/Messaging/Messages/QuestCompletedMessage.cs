using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Messaging.Messages
{
    public class QuestCompletedMessage
    {
        public NWPlayer Player { get; set; }
        public int QuestID { get; set; }

        public QuestCompletedMessage(NWPlayer player, int questID)
        {
            Player = player;
            QuestID = questID;
        }
    }
}

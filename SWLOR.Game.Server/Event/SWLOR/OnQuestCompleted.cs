using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnQuestCompleted
    {
        public NWPlayer Player { get; set; }
        public int QuestID { get; set; }

        public OnQuestCompleted(NWPlayer player, int questID)
        {
            Player = player;
            QuestID = questID;
        }
    }
}

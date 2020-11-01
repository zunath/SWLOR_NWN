using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnQuestAccepted
    {
        public NWPlayer Player { get; set; }
        public int QuestID { get; set; }

        public OnQuestAccepted(NWPlayer player, int questID)
        {
            Player = player;
            QuestID = questID;
        }
    }
}

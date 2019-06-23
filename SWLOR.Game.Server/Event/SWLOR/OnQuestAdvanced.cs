using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnQuestAdvanced
    {
        public NWPlayer Player { get; set; }
        public int QuestID { get; set; }
        public int Sequence { get; set; }

        public OnQuestAdvanced(NWPlayer player, int questID, int sequence)
        {
            Player = player;
            QuestID = questID;
            Sequence = sequence;
        }
    }
}

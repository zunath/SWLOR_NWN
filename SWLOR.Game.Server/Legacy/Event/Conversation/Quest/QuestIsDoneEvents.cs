using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.Quest
{
    public static class QuestIsDoneEvents
    {
        [NWNEventHandler("quest_done_1")]
        public static int QuestDone1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_2")]
        public static int QuestDone2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_3")]
        public static int QuestDone3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_4")]
        public static int QuestDone4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_5")]
        public static int QuestDone5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_6")]
        public static int QuestDone6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_7")]
        public static int QuestDone7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_8")]
        public static int QuestDone8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_9")]
        public static int QuestDone9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("quest_done_10")]
        public static int QuestDone10()
        {
            return Check(10) ? 1 : 0;
        }

        private static bool Check(int index)
        {
            NWPlayer player = NWScript.GetPCSpeaker();
            NWObject talkingTo = NWScript.OBJECT_SELF;
            var questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                NWScript.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (status == null) return false;

            var quest = QuestService.GetQuestByID(questID);
            var currentQuestState = quest.GetState(status.QuestState);
            var lastState = quest.GetStates().Last();
            return currentQuestState == lastState &&
                   status.CompletionDate != null;
        }
    }
}

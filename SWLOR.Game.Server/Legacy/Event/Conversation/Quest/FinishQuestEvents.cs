using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.Quest
{
    public static class FinishQuestEvents
    {
        [NWNEventHandler("finish_quest_1")]
        public static int FinishQuest1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_2")]
        public static int FinishQuest2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_3")]
        public static int FinishQuest3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_4")]
        public static int FinishQuest4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_5")]
        public static int FinishQuest5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_6")]
        public static int FinishQuest6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_7")]
        public static int FinishQuest7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_8")]
        public static int FinishQuest8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_9")]
        public static int FinishQuest9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("finish_quest_10")]
        public static int FinishQuest10()
        {
            return Check(10) ? 1 : 0;
        }
        private static bool Check(int index)
        {
            NWPlayer player = NWScript.GetPCSpeaker();
            NWObject talkTo = NWScript.OBJECT_SELF;
            var questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                NWScript.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            var quest = QuestService.GetQuestByID(questID);
            quest.Complete(player, talkTo, null);
            return true;
        }
    }
}

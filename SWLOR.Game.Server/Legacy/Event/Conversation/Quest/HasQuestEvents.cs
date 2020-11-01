using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.Quest
{
    public static class HasQuestEvents
    {
        [NWNEventHandler("has_quest_1")]
        public static int HasQuest1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_2")]
        public static int HasQuest2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_3")]
        public static int HasQuest3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_4")]
        public static int HasQuest4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_5")]
        public static int HasQuest5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_6")]
        public static int HasQuest6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_7")]
        public static int HasQuest7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_8")]
        public static int HasQuest8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_9")]
        public static int HasQuest9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("has_quest_10")]
        public static int HasQuest10()
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
            return status != null && status.QuestState > 0;
        
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class AcceptQuestEvents
    {
        [NWNEventHandler("accept_quest_1")]
        public static int AcceptQuest1()
        {
            return Check(1) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_2")]
        public static int AcceptQuest2()
        {
            return Check(2) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_3")]
        public static int AcceptQuest3()
        {
            return Check(3) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_4")]
        public static int AcceptQuest4()
        {
            return Check(4) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_5")]
        public static int AcceptQuest5()
        {
            return Check(5) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_6")]
        public static int AcceptQuest6()
        {
            return Check(6) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_7")]
        public static int AcceptQuest7()
        {
            return Check(7) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_8")]
        public static int AcceptQuest8()
        {
            return Check(8) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_9")]
        public static int AcceptQuest9()
        {
            return Check(9) ? 1 : 0;
        }

        [NWNEventHandler("accept_quest_10")]
        public static int AcceptQuest10()
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
            quest.Accept(player, talkTo);
        
            return true;
        }
    }
}

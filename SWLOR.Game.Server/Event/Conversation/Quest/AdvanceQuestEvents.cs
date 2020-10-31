using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class AdvanceQuestEvents
    {
        [NWNEventHandler("next_state_1")]
        public static int AdvanceQuest1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("next_state_2")]
        public static int AdvanceQuest2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("next_state_3")]
        public static int AdvanceQuest3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("next_state_4")]
        public static int AdvanceQuest4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("next_state_5")]
        public static int AdvanceQuest5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("next_state_6")]
        public static int AdvanceQuest6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("next_state_7")]
        public static int AdvanceQuest7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("next_state_8")]
        public static int AdvanceQuest8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("next_state_9")]
        public static int AdvanceQuest9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("next_state_10")]
        public static int AdvanceQuest10()
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
            quest.Advance(player, talkTo);
            
            return true;
        }
    }
}

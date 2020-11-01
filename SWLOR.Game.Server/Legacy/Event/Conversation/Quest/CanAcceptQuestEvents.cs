using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.Quest
{
    public static class CanAcceptQuestEvents
    {
        [NWNEventHandler("can_accept_1")]
        public static int CanAccept1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_2")]
        public static int CanAccept2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_3")]
        public static int CanAccept3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_4")]
        public static int CanAccept4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_5")]
        public static int CanAccept5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_6")]
        public static int CanAccept6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_7")]
        public static int CanAccept7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_8")]
        public static int CanAccept8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_9")]
        public static int CanAccept9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("can_accept_10")]
        public static int CanAccept10()
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
            return quest.CanAccept(player);
        
        }
    }
}

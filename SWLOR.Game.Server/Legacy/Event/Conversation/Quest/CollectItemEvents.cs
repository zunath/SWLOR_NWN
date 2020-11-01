using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Event.Conversation.Quest
{
    public static class CollectItemEvents
    {
        [NWNEventHandler("collect_item_1")]
        public static int CollectItem1()
        {
            return Check(1) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_2")]
        public static int CollectItem2()
        {
            return Check(2) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_3")]
        public static int CollectItem3()
        {
            return Check(3) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_4")]
        public static int CollectItem4()
        {
            return Check(4) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_5")]
        public static int CollectItem5()
        {
            return Check(5) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_6")]
        public static int CollectItem6()
        {
            return Check(6) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_7")]
        public static int CollectItem7()
        {
            return Check(7) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_8")]
        public static int CollectItem8()
        {
            return Check(8) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_9")]
        public static int CollectItem9()
        {
            return Check(9) ? 1 : 0;
        }
        [NWNEventHandler("collect_item_10")]
        public static int CollectItem10()
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

            QuestService.RequestItemsFromPC(player, talkTo, questID);

            return true;
        
        }
    }
}

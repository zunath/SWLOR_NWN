using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.CollectQuestItem
{
    public static class QuestCollectItem
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCollectItem)))
            {
                int index = (int) args[0];
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject talkTo = NWScript.OBJECT_SELF;
                int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
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
}

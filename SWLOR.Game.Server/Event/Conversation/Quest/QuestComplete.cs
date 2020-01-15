using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class QuestComplete
    {
        public static bool Check(int index, int customRuleIndex)
        {
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = NWGameObject.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            var quest = QuestService.GetQuestByID(questID);
            quest.Complete(player, talkTo, null);
            return true;
        }
    }
}

using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest
{
    public static class QuestComplete
    {
        public static bool Check(int index, int customRuleIndex)
        {
            using (new Profiler(nameof(QuestComplete) + ".Index" + index + ".Rule" + customRuleIndex))
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
}

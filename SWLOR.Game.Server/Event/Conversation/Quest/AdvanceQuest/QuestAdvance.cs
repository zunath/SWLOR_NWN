using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Event.Conversation.Quest.CanAcceptQuest;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.AdvanceQuest
{
    public static class QuestAdvance
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCanAccept)))
            {
                var index = (int) args[0];
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
            }

            return true;
        }
    }
}

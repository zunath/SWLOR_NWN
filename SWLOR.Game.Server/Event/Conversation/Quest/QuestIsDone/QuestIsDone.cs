using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.QuestIsDone
{
    public static class QuestIsDone
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestIsDone)))
            {
                int index = (int) args[0];
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkingTo = NWGameObject.OBJECT_SELF;
                int questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

                if (!QuestService.QuestExistsByID(questID))
                {
                    _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
                if (status == null) return false;

                var quest = QuestService.GetQuestByID(questID);
                var currentQuestState = quest.GetState(status.QuestState);
                var lastState = quest.GetStates().Last();
                return currentQuestState == lastState &&
                       status.CompletionDate != null;
            }
        }
    }
}

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

                if (!DataService.Quest.ExistsByID(questID))
                {
                    _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
                if (status == null) return false;


                var currentQuestState = DataService.QuestState.GetByID(status.CurrentQuestStateID);
                var states = DataService.QuestState.GetAllByQuestID(currentQuestState.QuestID);
                return currentQuestState.ID == states.OrderBy(o => o.Sequence).Last().ID &&
                       status.CompletionDate != null;
            }
        }
    }
}

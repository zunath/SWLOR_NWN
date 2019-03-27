using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Events.Conversation.Quest.CollectQuestItem;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.NWN.Events.Conversation.Quest.QuestIsDone
{
    public static class QuestIsDone
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestIsDone)))
            {
                int index = (int) args[0];
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkingTo = Object.OBJECT_SELF;
                int questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

                if (DataService.GetAll<Data.Entity.Quest>().All(x => x.ID != questID))
                {
                    _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                var status = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
                if (status == null) return false;


                var currentQuestState = DataService.Get<QuestState>(status.CurrentQuestStateID);
                var quest = DataService.Get<Data.Entity.Quest>(currentQuestState.QuestID);
                var states = DataService.Where<QuestState>(x => x.QuestID == quest.ID);
                return currentQuestState.ID == states.OrderBy(o => o.Sequence).Last().ID &&
                       status.CompletionDate != null;
            }
        }
    }
}

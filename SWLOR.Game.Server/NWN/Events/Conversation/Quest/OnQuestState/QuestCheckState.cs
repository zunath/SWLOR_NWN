using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using Object = NWN.Object;

namespace SWLOR.Game.Server.NWN.Events.Conversation.Quest.OnQuestState
{
    public static class QuestCheckState
    {
        public static bool Check(params object[] args)
        {
            int index = (int) args[0];
            int state = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (DataService.GetAll<Data.Entity.Quest>().All(x => x.ID != questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                return false;
            }
            
            var status = DataService.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
            if (status == null) return false;
            
            var questState = DataService.Get<QuestState>(status.CurrentQuestStateID);

            bool has = questState.Sequence == state && status.CompletionDate == null;
            return has;
        }
    }
}

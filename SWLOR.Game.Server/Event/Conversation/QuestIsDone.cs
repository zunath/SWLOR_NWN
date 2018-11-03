using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestIsDone : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public QuestIsDone(
            INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            int index = (int)args[0];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkingTo = Object.OBJECT_SELF;
            int questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

            if (!_data.Quests.Any(x => x.QuestID == questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            var status = _data.PCQuestStatus.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
            return status != null && 
                   status.CurrentQuestState.QuestStateID == status.CurrentQuestState.Quest.QuestStates.OrderBy(o => o.Sequence).Last().QuestStateID &&
                   status.CompletionDate != null;
        }
    }
}

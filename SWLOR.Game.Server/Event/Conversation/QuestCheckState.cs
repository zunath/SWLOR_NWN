using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestCheckState : IRegisteredEvent
    {
        
        private readonly IQuestService _quest;
        private readonly IDataService _data;

        public QuestCheckState(
            
            IQuestService quest,
            IDataService data)
        {
            
            _quest = quest;
            _data = data;
        }

        public bool Run(params object[] args)
        {
            int index = (int) args[0];
            int state = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (_data.GetAll<Quest>().All(x => x.ID != questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                return false;
            }
            
            var status = _data.SingleOrDefault<PCQuestStatus>(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
            if (status == null) return false;
            
            var questState = _data.Get<QuestState>(status.CurrentQuestStateID);

            bool has = questState.Sequence == state && status.CompletionDate == null;
            return has;
        }
    }
}

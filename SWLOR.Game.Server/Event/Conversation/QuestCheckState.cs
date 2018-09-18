using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestCheckState : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;
        private readonly IDataContext _db;

        public QuestCheckState(
            INWScript script,
            IQuestService quest,
            IDataContext db)
        {
            _ = script;
            _quest = quest;
            _db = db;
        }

        public bool Run(params object[] args)
        {
            int index = (int) args[0];
            int state = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!_db.Quests.Any(x => x.QuestID == questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                return false;
            }
            
            var status = _db.PCQuestStatus.SingleOrDefault(x => x.PlayerID == player.GlobalID && x.QuestID == questID);
            bool has = status != null && status.CurrentQuestState.Sequence == state && status.CompletionDate == null;
            return has;
        }
    }
}

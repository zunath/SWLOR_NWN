using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestComplete: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;
        private readonly IDataContext _db;

        public QuestComplete(
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
            int index = (int)args[0];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!_db.Quests.Any(x => x.QuestID == questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            _quest.CompleteQuest(player, talkTo ,questID, null);

            return true;
        }
    }
}

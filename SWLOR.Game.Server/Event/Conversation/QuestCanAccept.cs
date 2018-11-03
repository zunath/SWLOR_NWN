using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestCanAccept : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;
        private readonly IDataService _data;

        public QuestCanAccept(
            INWScript script,
            IQuestService quest,
            IDataService data)
        {
            _ = script;
            _quest = quest;
            _data = data;
        }
        public bool Run(params object[] args)
        {
            int index = (int)args[0];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!_data.Quests.Any(x => x.QuestID == questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            return _quest.CanAcceptQuest(player, questID, false);
        }
    }
}

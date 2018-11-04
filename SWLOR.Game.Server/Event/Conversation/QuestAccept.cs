using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestAccept: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;
        private readonly IDataService _data;

        public QuestAccept(
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
            int index = (int) args[0];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (_data.GetAll<Quest>().All(x => x.QuestID != questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }
            _quest.AcceptQuest(player, talkTo, questID);

            return true;
        }
    }
}

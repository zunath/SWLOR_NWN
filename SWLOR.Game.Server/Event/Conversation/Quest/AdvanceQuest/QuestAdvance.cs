using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Conversation.Quest.CanAcceptQuest;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.AdvanceQuest
{
    public static class QuestAdvance
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCanAccept)))
            {
                int index = (int) args[0];
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkTo = NWGameObject.OBJECT_SELF;
                int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

                if (!DataService.Quest.ExistsByID(questID))
                {
                    _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                QuestService.AdvanceQuestState(player, talkTo, questID);
            }

            return true;
        }
    }
}

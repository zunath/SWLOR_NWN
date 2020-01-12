using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class QuestCheck
    {
        public static bool Check(params object[] args)
        {
            int index = (int) args[0];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkingTo = NWGameObject.OBJECT_SELF;
            int questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                return false;
            }

            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            return status != null && status.QuestState > 0;
        }
    }
}

using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Event.Conversation.Quest
{
    public static class QuestCheckState
    {
        public static bool Check(params object[] args)
        {
            int index = (int) args[0];
            int state = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = NWGameObject.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (!QuestService.QuestExistsByID(questID))
            {
                _.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                return false;
            }

            var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
            if (status == null) return false;

            bool has = status.QuestState == state && status.CompletionDate == null;
            return has;
        
        }
    }
}

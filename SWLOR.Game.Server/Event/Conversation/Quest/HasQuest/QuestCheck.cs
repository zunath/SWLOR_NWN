using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Legacy;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.HasQuest
{
    public static class QuestCheck
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCheck)))
            {
                var index = (int) args[0];
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject talkingTo = NWScript.OBJECT_SELF;
                var questID = talkingTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkingTo.GetLocalInt("QST_ID_" + index);

                if (!QuestService.QuestExistsByID(questID))
                {
                    NWScript.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
                return status != null && status.QuestState > 0;
            }
        }
    }
}

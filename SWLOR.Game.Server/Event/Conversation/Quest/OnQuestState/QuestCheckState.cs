using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.OnQuestState
{
    public static class QuestCheckState
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCheckState)))
            {
                var index = (int) args[0];
                var state = (int) args[1];
                NWPlayer player = NWScript.GetPCSpeaker();
                NWObject talkTo = NWScript.OBJECT_SELF;
                var questID = talkTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

                if (!QuestService.QuestExistsByID(questID))
                {
                    NWScript.SpeakString("ERROR: Quest #" + index + " State #" + state + " is improperly configured. Please notify an admin");
                    return false;
                }

                var status = DataService.PCQuestStatus.GetByPlayerAndQuestIDOrDefault(player.GlobalID, questID);
                if (status == null) return false;

                var has = status.QuestState == state && status.CompletionDate == null;
                return has;
            }
        }
    }
}

﻿using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.HasQuest
{
    public static class QuestCheck
    {
        public static bool Check(params object[] args)
        {
            using (new Profiler(nameof(QuestCheck)))
            {
                int index = (int) args[0];
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkingTo = _.OBJECT_SELF;
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
}

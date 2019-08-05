using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Event.Conversation.Quest.FinishQuest
{
    public static class QuestComplete
    {
        public static bool Check(int index, int customRuleIndex)
        {
            using (new Profiler(nameof(QuestComplete) + ".Index" + index + ".Rule" + customRuleIndex))
            {
                NWPlayer player = _.GetPCSpeaker();
                NWObject talkTo = NWGameObject.OBJECT_SELF;
                int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
                if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

                if (!DataService.Quest.ExistsByID(questID))
                {
                    _.SpeakString("ERROR: Quest #" + index + " is improperly configured. Please notify an admin");
                    return false;
                }

                string rule = string.Empty;
                string ruleArgs = string.Empty;
                if (customRuleIndex > 0)
                {
                    string ruleName = "QUEST_ID_" + index + "_RULE_" + customRuleIndex;
                    rule = talkTo.GetLocalString(ruleName);
                    ruleArgs = talkTo.GetLocalString("QUEST_ID_" + index + "_RULE_ARGS_" + customRuleIndex);

                    if (string.IsNullOrWhiteSpace(rule))
                    {
                        _.SpeakString("ERROR: Quest #" + index + ", rule #" + customRuleIndex + " is improperly configured. Please notify an admin.");
                        return false;
                    }
                }

                QuestService.CompleteQuest(player, talkTo, questID, null);

                if (!string.IsNullOrWhiteSpace(rule))
                {
                    Data.Entity.Quest quest = DataService.Quest.GetByID(questID);
                    var ruleAction = QuestService.GetQuestRule(rule);

                    string[] argsArray = null;

                    if (string.IsNullOrWhiteSpace(ruleArgs))
                    {
                        ruleArgs = quest.OnCompleteArgs;
                    }

                    if (!string.IsNullOrWhiteSpace(ruleArgs))
                    {
                        argsArray = ruleArgs.Split(',');
                    }

                    ruleAction.Run(player, talkTo, questID, argsArray);

                }

                return true;
            }
        }
    }
}

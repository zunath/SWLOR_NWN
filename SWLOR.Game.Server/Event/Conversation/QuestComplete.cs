using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Conversation
{
    public class QuestComplete: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IQuestService _quest;
        private readonly IDataService _data;

        public QuestComplete(
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
            int customRuleIndex = (int) args[1];
            NWPlayer player = _.GetPCSpeaker();
            NWObject talkTo = Object.OBJECT_SELF;
            int questID = talkTo.GetLocalInt("QUEST_ID_" + index);
            if (questID <= 0) questID = talkTo.GetLocalInt("QST_ID_" + index);

            if (_data.GetAll<Quest>().All(x => x.QuestID != questID))
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
            
            _quest.CompleteQuest(player, talkTo, questID, null);

            if (!string.IsNullOrWhiteSpace(rule))
            {
                Quest quest = _data.Single<Quest>(x => x.QuestID == questID);
                App.ResolveByInterface<IQuestRule>("QuestRule." + rule, ruleAction =>
                {
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
                });
                
            }

            return true;
        }
    }
}

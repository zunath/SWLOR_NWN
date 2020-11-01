using System.Linq;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Quest.Contracts;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class QuestRewardSelection: ConversationBase
    {
        private class Model
        {
            public int QuestID { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                "Please select a reward."
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            var questID = GetPC().GetLocalInt("QST_REWARD_SELECTION_QUEST_ID");
            GetPC().DeleteLocalInt("QST_REWARD_SELECTION_QUEST_ID");
            var quest = QuestService.GetQuestByID(questID);
            var rewardItems = quest.GetRewards().Where(x => x.IsSelectable);
            
            var model = GetDialogCustomData<Model>();
            model.QuestID = questID;
            
            foreach (var reward in rewardItems)
            {
                AddResponseToPage("MainPage", reward.MenuName, true, reward);
            }

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                {
                    HandleRewardSelection(responseID);
                    break;
                }
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleRewardSelection(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var reward = GetResponseByID("MainPage", responseID).CustomData as IQuestReward;
            var quest = QuestService.GetQuestByID(model.QuestID);
            quest.Complete(GetPC(), GetPC(), reward);
            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}

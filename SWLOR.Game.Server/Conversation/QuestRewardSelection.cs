using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class QuestRewardSelection: ConversationBase
    {
        private class Model
        {
            public int QuestID { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "Please select a reward."
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            int questID = GetPC().GetLocalInt("QST_REWARD_SELECTION_QUEST_ID");
            GetPC().DeleteLocalInt("QST_REWARD_SELECTION_QUEST_ID");
            var quest = QuestService.GetQuestByID(questID);
            var rewardItems = quest.GetRewards();
            
            Model model = GetDialogCustomData<Model>();
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
            Model model = GetDialogCustomData<Model>();
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

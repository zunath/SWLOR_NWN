using System;
using System.Linq;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Conversation
{
    public class QuestRewardSelection: ConversationBase
    {
        private class Model
        {
            public int QuestID { get; set; }
        }
        
        private readonly IQuestService _quest;
        private readonly IDataService _data;

        public QuestRewardSelection(
            INWScript script, 
            IDialogService dialog,
            IQuestService quest,
            IDataService data) 
            : base(script, dialog)
        {
            _quest = quest;
            _data = data;
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
            Quest quest = _quest.GetQuestByID(questID);
            var rewardItems = _data.Where<QuestRewardItem>(x => x.QuestID == quest.ID);

            Model model = GetDialogCustomData<Model>();
            model.QuestID = questID;
            
            foreach (QuestRewardItem reward in rewardItems)
            {
                ItemVO tempItem = _quest.GetTempItemInformation(reward.Resref, reward.Quantity);
                string rewardName = tempItem.Name;
                if (tempItem.Quantity > 1)
                    rewardName += " x" + tempItem.Quantity;
                
                AddResponseToPage("MainPage", rewardName, true, tempItem);
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
            ItemVO tempItem = (ItemVO)GetResponseByID("MainPage", responseID).CustomData;
            _quest.CompleteQuest(GetPC(), null, model.QuestID, tempItem);

            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}

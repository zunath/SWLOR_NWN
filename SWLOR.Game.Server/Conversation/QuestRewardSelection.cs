using System;
using System.Linq;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class QuestRewardSelection: ConversationBase
    {
        private class Model
        {
            public int QuestID { get; set; }
            public bool IsItemSelected { get; set; }
        }
        
        private readonly IQuestService _quest;

        public QuestRewardSelection(
            INWScript script, 
            IDialogService dialog,
            IQuestService quest) 
            : base(script, dialog)
        {
            _quest = quest;
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
            
            Model model = GetDialogCustomData<Model>();
            model.IsItemSelected = false;
            model.QuestID = questID;
            
            foreach (QuestRewardItem reward in quest.QuestRewardItems)
            {
                ItemVO tempItem = _quest.GetTempItemInformation(reward.Resref, reward.Quantity);
                string rewardName = tempItem.Name;
                if (tempItem.Quantity > 1)
                    rewardName += " x" + tempItem.Quantity;
                
                AddResponseToPage("MainPage", rewardName, true, new Tuple<string, dynamic>(string.Empty, tempItem));
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

        private void HandleRewardSelection(int responseID)
        {
            Model model = GetDialogCustomData<Model>();
            ItemVO tempItem = (ItemVO)GetResponseByID("MainPage", responseID).CustomData[string.Empty];
            Quest quest = _quest.GetQuestByID(model.QuestID);

            QuestState lastState = quest.QuestStates.Last();
            _.AddJournalQuestEntry(quest.JournalTag, lastState.JournalStateID, GetPC().Object, NWScript.FALSE);
            _quest.CompleteQuest(GetPC(), model.QuestID, tempItem);
            model.IsItemSelected = true;

            EndConversation();
        }

        public override void EndDialog()
        {
            Model model = GetDialogCustomData<Model>();
            Quest quest = _quest.GetQuestByID(model.QuestID);

            if (!model.IsItemSelected)
            {
                // TODO: Return submitted items.
            }
        }
    }
}

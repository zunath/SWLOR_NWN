using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation.NPCs
{
    public class WariersDatson: ConversationBase
    {
        private readonly IQuestService _quest;

        public WariersDatson(INWScript script, 
            IDialogService dialog, 
            IQuestService quest) 
            : base(script, dialog)
        {
            _quest = quest;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Careful, citizen. There are many Dezmites skittering around outside this gate.",
                "[QUEST] Dezmite Patrol");

            DialogPage dezmitePatrol = new DialogPage("We're a little undermanned right now and need some help culling back the Dezmites. If you're headed through, why not kill a few along the way?",
                "[ACCEPT QUEST] Sure, I'll kill a few Dezmites for you.",
                "Back");
            DialogPage dezmitePatrolWIP = new DialogPage("How goes the hunt? Have you killed ten Dezmites yet?",
                "I'm still working on it.",
                "Back");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("DezmitePatrol", dezmitePatrol);
            dialog.AddPage("DezmitePatrolWIP", dezmitePatrolWIP);

            return dialog;
        }

        public override void Initialize()
        {
            RefreshConvoOptions();
        }

        private void RefreshConvoOptions()
        {
            if (_quest.HasPlayerCompletedQuest(GetPC(), QuestID.DezmitePatrol))
            {
                GetResponseByID("MainPage", 1).IsActive = false;
            }

            if (_quest.GetPlayerQuestJournalID(GetPC(), QuestID.DezmitePatrol) == 2)
            {
                GetResponseByID("DezmitePatrolWIP", 1).Text = "It was dirty work, but I've slain ten Dezmites.";
            }

        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponses(responseID);
                    break;
                case "DezmitePatrol":
                    HandleDezmitePatrolResponses(responseID);
                    break;
                case "DezmitePatrolWIP":
                    HandleDezmitePatrolWIPResponses(responseID);
                    break;
            }
        }

        private void HandleMainPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    if (_quest.GetPlayerQuestJournalID(GetPC(), QuestID.DezmitePatrol) == -1)
                        ChangePage("DezmitePatrolPage");
                    else ChangePage("DezmitePatrolWIPPage");
                    break;
            }
        }

        private void HandleDezmitePatrolResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    _quest.AcceptQuest(GetPC(), QuestID.DezmitePatrol);
                    RefreshConvoOptions();
                    ChangePage("MainPage");
                    break;
                case 2:
                    ChangePage("MainPage");
                    break;
            }
        }

        private void HandleDezmitePatrolWIPResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    if (_quest.GetPlayerQuestJournalID(GetPC(), QuestID.DezmitePatrol) == 2)
                    {
                        _quest.AdvanceQuestState(GetPC(), QuestID.DezmitePatrol);
                        RefreshConvoOptions();
                    }
                    ChangePage("MainPage");
                    break;
                case 2:
                    ChangePage("MainPage");
                    break;
            }
        }

        public override void EndDialog()
        {
        }
    }
}

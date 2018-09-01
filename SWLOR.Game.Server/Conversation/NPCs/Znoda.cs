using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation.NPCs
{
    public class Znoda: ConversationBase
    {
        public Znoda(INWScript script, IDialogService dialog) 
            : base(script, dialog)
        {
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "A warm greetings to you. How may I be of service?",
                "What do you have for sale?",
                "Tell me about yourself.");

            DialogPage infoPage = new DialogPage(
                "I am Z'Noda Klibi, a Zuke. I work as a provisioner for Expedition Corp.",
                "What is a Zuke?",
                "What is Expedition Corp?",
                "What do you have for sale?");

            DialogPage whatIsAZuke = new DialogPage(
                "Zukes are a species originating from the planet New Zuken. One of our defining features are the tentacles on our mouth. We can use these to alter the minds of others. However, the Pangalactic Federation has outlawed its use so you don't need to worry about that!",
                "I have other questions.",
                "What do you have for sale?");

            DialogPage whatIsExpeditionCorp = new DialogPage(
                "Expedition Corp is a company focused in harvesting, mining, and other procurement activities around the galaxy. We recently arrived on Druzer IX after surveying the planet and discovering it had raw materials. Unfortunately for us, several other competing companies have also shown up.",
                "I have other questions.",
                "What do you have for sale?");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("InfoPage", infoPage);
            dialog.AddPage("WhatIsAZuke", whatIsAZuke);
            dialog.AddPage("WhatIsExpeditionCorp", whatIsExpeditionCorp);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponses(responseID);
                    break;
                case "InfoPage":
                    HandleInfoPageResponses(responseID);
                    break;
                case "WhatIsAZuke":
                    HandleWhatIsAZukePageResponses(responseID);
                    break;
                case "WhatIsExpeditionCorp":
                    HandleWhatIsExpeditionCorpPageResponses(responseID);
                    break;
            }
        }

        private void HandleMainPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    OpenStore();
                    break;
                case 2:
                    ChangePage("InfoPage");
                    break;
            }
        }

        private void HandleInfoPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    ChangePage("WhatIsAZuke");
                    break;
                case 2:
                    ChangePage("WhatIsExpeditionCorp");
                    break;
                case 3:
                    OpenStore();
                    break;
            }
        }

        private void HandleWhatIsAZukePageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    ChangePage("InfoPage");
                    break;
                case 2:
                    OpenStore();
                    break;
            }

        }

        private void HandleWhatIsExpeditionCorpPageResponses(int responseID)
        {
            switch (responseID)
            {
                case 1:
                    ChangePage("InfoPage");
                    break;
                case 2:
                    OpenStore();
                    break;
            }

        }

        private void OpenStore()
        {
            Object store = _.GetNearestObject(NWScript.OBJECT_TYPE_STORE, Object.OBJECT_SELF);
            _.OpenStore(store, GetPC().Object);
            EndConversation();
        }

        public override void EndDialog()
        {
        }
    }
}

using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class ChangeDescription: ConversationBase
    {

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Next"
            );

            DialogPage confirmSetPage = new DialogPage(
                "<SET LATER>",
                "Confirm Description Change"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ConfirmSetPage", confirmSetPage);
            return dialog;
        }

        public override void Initialize()
        {
            string header = "Please type the new description for your character into the chat box. Then press the 'Next' button.\n\n";
            header += ColorTokenService.Green("Current Description: ") + "\n\n";
            header += _.GetDescription(GetPC().Object);
            SetPageHeader("MainPage", header);
            GetPC().SetLocalInt("LISTENING_FOR_DESCRIPTION", 1);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageResponse(responseID);
                    break;
                case "ConfirmSetPage":
                    HandleConfirmSetPageResponse(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Next
                    string newDescription = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");

                    if (string.IsNullOrWhiteSpace(newDescription))
                    {
                        _.FloatingTextStringOnCreature("Type in a new description to the chat bar and then press 'Next'.", GetPC().Object, _.FALSE);
                        return;
                    }

                    string header = "Your new description follows. If you need to make a change, click 'Back', type in a new description, and then hit 'Next' again.\n\n";
                    header += ColorTokenService.Green("New Description: ") + "\n\n";
                    header += newDescription;
                    SetPageHeader("ConfirmSetPage", header);
                    ChangePage("ConfirmSetPage");
                    break;
            }
        }

        private void HandleConfirmSetPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm Description Change
                    PlayerDescriptionService.ChangePlayerDescription(GetPC());
                    EndConversation();
                    break;
            }
        }
        public override void EndDialog()
        {
            GetPC().DeleteLocalInt("LISTENING_FOR_DESCRIPTION");
            GetPC().DeleteLocalString("NEW_DESCRIPTION_TO_SET");
        }
    }
}

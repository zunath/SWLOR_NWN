using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class ChangeDescription: ConversationBase
    {
        private readonly IColorTokenService _color;
        private readonly IPlayerDescriptionService _playerDescription;

        public ChangeDescription(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color,
            IPlayerDescriptionService playerDescription) 
            : base(script, dialog)
        {
            _color = color;
            _playerDescription = playerDescription;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Next",
                "Back"
            );

            DialogPage confirmSetPage = new DialogPage(
                "<SET LATER>",
                "Confirm Description Change",
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ConfirmSetPage", confirmSetPage);
            return dialog;
        }

        public override void Initialize()
        {
            string header = "Please type the new description for your character into the chat box. Then press the 'Next' button.\n\n";
            header += _color.Green("Current Description: ") + "\n\n";
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

        private void HandleMainPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Next
                    string newDescription = GetPC().GetLocalString("NEW_DESCRIPTION_TO_SET");

                    if (string.IsNullOrWhiteSpace(newDescription))
                    {
                        _.FloatingTextStringOnCreature("Type in a new description to the chat bar and then press 'Next'.", GetPC().Object, NWScript.FALSE);
                        return;
                    }

                    string header = "Your new description follows. If you need to make a change, click 'Back', type in a new description, and then hit 'Next' again.\n\n";
                    header += _color.Green("New Description: ") + "\n\n";
                    header += newDescription;
                    SetPageHeader("ConfirmSetPage", header);
                    ChangePage("ConfirmSetPage");
                    break;
                case 2: // Back
                    SwitchConversation("CharacterManagement");
                    break;
            }
        }

        private void HandleConfirmSetPageResponse(int responseID)
        {
            switch (responseID)
            {
                case 1: // Confirm Description Change
                    _playerDescription.ChangePlayerDescription(GetPC());
                    EndConversation();
                    break;
                case 2: // Back
                    ChangePage("MainPage");
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

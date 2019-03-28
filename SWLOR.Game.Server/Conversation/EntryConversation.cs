using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class EntryConversation: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("Are you ready to enter the game world? This is the LAST chance for you to use the '/customize' chat command to change your appearance.\n\nAre you sure you want to proceed?",
                "Customize my character",
                "Enter the game");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (responseID)
            {
                case 1: // Customize my character
                    SwitchConversation("CharacterCustomization");
                    break;
                case 2: // Enter the game
                    NWObject waypoint = _.GetObjectByTag("ENTRY_STARTING_WP");
                    player.AssignCommand(() =>
                    {
                        _.ActionJumpToLocation(waypoint.Location);
                    });
                    EndConversation();
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}

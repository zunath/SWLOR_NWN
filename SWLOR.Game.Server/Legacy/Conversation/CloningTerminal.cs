using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;

namespace SWLOR.Game.Server.Legacy.Conversation
{
    public class CloningTerminal: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            var dialog = new PlayerDialog("MainPage");
            var mainPage = new DialogPage(
                "If you die, you will return to the last medical facility you registered at. Would you like to register to this medical facility?",
                ColorTokenService.Green("Register")
            );

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            if (responseID == 1)
            {
                DeathService.SetRespawnLocation(player);
                EndConversation();
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

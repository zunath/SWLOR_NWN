using NWN;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class CloningTerminal: ConversationBase
    {
        private readonly IDeathService _death;
        private readonly IColorTokenService _color;

        public CloningTerminal(
            INWScript script, 
            IDialogService dialog,
            IDeathService death,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _death = death;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "If you die, you will return to the last cloning facility you registered at. Would you like to register to this cloning facility?",
                _color.Green("Register")
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
                _death.SetRespawnLocation(player);
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

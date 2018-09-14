using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Customizes your character appearance. Only available for use in the entry area.", CommandPermissionType.Player)]
    public class Customize: IChatCommand
    {
        private readonly IDialogService _dialog;

        public Customize(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public void DoAction(NWPlayer user, params string[] args)
        {
            _dialog.StartConversation(user, user, "CharacterCustomization");
        }
    }
}

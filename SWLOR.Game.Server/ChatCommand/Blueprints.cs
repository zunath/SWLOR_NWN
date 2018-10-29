using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Opens the blueprints menu.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Blueprints : IChatCommand
    {
        private readonly IDialogService _dialog;

        public Blueprints(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            _dialog.StartConversation(user, user, "ViewBlueprints");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Opens the skills menu.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Skills : IChatCommand
    {
        private readonly IDialogService _dialog;

        public Skills(IDialogService dialog)
        {
            _dialog = dialog;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            _dialog.StartConversation(user, user, "ViewSkills");
        }

        public bool RequiresTarget => false;
    }
}

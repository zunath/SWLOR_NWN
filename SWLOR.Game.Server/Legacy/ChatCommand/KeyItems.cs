using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Opens the key items menu.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class KeyItems : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            DialogService.StartConversation(user, user, "KeyItems");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

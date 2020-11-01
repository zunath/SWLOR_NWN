using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Copies the targeted item.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class CopyItem : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.ObjectType != ObjectType.Item)
            {
                user.SendMessage("You can only copy items with this command.");
                return;
            }

            NWScript.CopyItem(target, user, true);
            user.SendMessage("Item copied successfully.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

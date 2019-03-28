using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Copies the targeted item.", CommandPermissionType.DM)]
    public class CopyItem : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.ObjectType != OBJECT_TYPE_ITEM)
            {
                user.SendMessage("You can only copy items with this command.");
                return;
            }

            _.CopyItem(target, user, TRUE);
            user.SendMessage("Item copied successfully.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

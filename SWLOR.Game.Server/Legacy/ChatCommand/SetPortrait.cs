using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Sets portrait of the target player using the string specified. (Remember to add po_ to the portrait)", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class SetPortrait : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid || target.ObjectType != ObjectType.Creature)
            {
                user.SendMessage("Only creatures may be targeted with this command.");
                return;
            }

            NWPlayer player = target.Object;
            NWScript.SetPortraitResRef(player, args[0]);
            player.FloatingText("Your portrait has been changed.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return "Please enter the name of the portrait and try again. Example: /SetPortrait po_myportrait";
            }

            if (args[0].Length > 16)
            {
                return "The portrait you entered is too long. Portrait names should be between 1 and 16 characters.";
            }


            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

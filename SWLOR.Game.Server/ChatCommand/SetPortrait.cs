using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;



namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets portrait of the target player using the string specified. (Remember to add po_ to the portrait)", CommandPermissionType.DM)]
    public class SetPortrait : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid || target.ObjectType != _.OBJECT_TYPE_CREATURE)
            {
                user.SendMessage("Only creatures may be targeted with this command.");
                return;
            }

            NWPlayer player = target.Object;
            _.SetPortraitResRef(player, args[0]);
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

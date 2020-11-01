using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Changes the name of a target.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Name : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.IsPlayer || target.IsDM)
            {
                user.SendMessage("PCs cannot be targeted with this command.");
                return;
            }

            var name = string.Empty;
            foreach (var arg in args)
            {
                name += " " + arg;
            }

            target.Name = name;
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return "Please enter a name. Example: /name My Creature";
            }
            
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

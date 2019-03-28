using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Changes the name of a target.", CommandPermissionType.DM)]
    public class Name : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.IsPlayer || target.IsDM)
            {
                user.SendMessage("PCs cannot be targeted with this command.");
                return;
            }

            string name = string.Empty;
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

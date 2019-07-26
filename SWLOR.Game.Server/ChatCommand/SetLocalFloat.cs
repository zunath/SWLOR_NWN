using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets a local float on a target.", CommandPermissionType.DM)]
    public class SetLocalFloat : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            string variableName = args[0];
            float value = float.Parse(args[1]);

            _.SetLocalFloat(target, variableName, value);

            user.SendMessage("Local float set: " + variableName + " = " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 2)
            {
                return "Missing arguments. Format should be: /SetLocalFloat Variable_Name <VALUE>. Example: /SetLocalFloat MY_VARIABLE 6.9";
            }
            
            if (!float.TryParse(args[1], out var value))
            {
                return "Invalid value entered. Please try again.";
            }
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

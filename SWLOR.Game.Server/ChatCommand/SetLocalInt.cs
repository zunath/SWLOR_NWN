using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets a local integer on a target.", CommandPermissionType.DM)]
    public class SetLocalInt : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            string variableName = args[0];
            int value = Convert.ToInt32(args[1]);

            _.SetLocalInt(target, variableName, value);

            user.SendMessage("Local integer set: " + variableName + " = " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 2)
            {
                return "Missing arguments. Format should be: /SetLocalInt Variable_Name <VALUE>. Example: /SetLocalInt MY_VARIABLE 69";
            }

            if (!int.TryParse(args[1], out var value))
            {
                return "Invalid value entered. Please try again.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

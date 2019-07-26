using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets a local string on a target.", CommandPermissionType.DM)]
    public class SetLocalString : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            string variableName = Convert.ToString(args[0]);
            string value = string.Empty;

            for (int x = 1; x < args.Length; x++)
            {
                value += " " + args[x];
            }

            value = value.Trim();

            _.SetLocalString(target, variableName, value);

            user.SendMessage("Local string set: " + variableName + " = " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /SetLocalString Variable_Name <VALUE>. Example: /SetLocalString MY_VARIABLE My Text";
            }
            
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

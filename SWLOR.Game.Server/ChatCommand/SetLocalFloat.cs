using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets a local float on a target.", CommandPermissionType.DM)]
    public class SetLocalFloat : IChatCommand
    {
        private readonly INWScript _;

        public SetLocalFloat(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 2)
            {
                user.SendMessage("Missing arguments. Format should be: /SetLocalFloat Variable_Name <VALUE>. Example: /SetLocalFloat MY_VARIABLE 6.9");
                return;
            }

            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid.");
                return;
            }

            string variableName = Convert.ToString(args[0]);

            if (!float.TryParse(args[1], out var value))
            {
                user.SendMessage("Invalid value entered. Please try again.");
                return;
            }

            _.SetLocalFloat(target, variableName, value);

            user.SendMessage("Local float set: " + variableName + " = " + value);
        }

        public bool RequiresTarget => true;
    }
}

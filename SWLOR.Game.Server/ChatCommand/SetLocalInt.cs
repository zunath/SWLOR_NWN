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
        private readonly INWScript _;

        public SetLocalInt(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 2)
            {
                user.SendMessage("Missing arguments. Format should be: /SetLocalInt Variable_Name <VALUE>. Example: /SetLocalInt MY_VARIABLE 69");
                return;
            }

            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid.");
                return;
            }

            string variableName = Convert.ToString(args[0]);

            if (!int.TryParse(args[1], out var value))
            {
                user.SendMessage("Invalid value entered. Please try again.");
                return;
            }

            _.SetLocalInt(target, variableName, value);

            user.SendMessage("Local integer set: " + variableName + " = " + value);
        }

        public bool RequiresTarget => true;
    }
}

using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets a local integer on a target.", CommandPermissionType.DM)]
    public class GetLocalInt : IChatCommand
    {
        private readonly INWScript _;

        public GetLocalInt(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 1)
            {
                user.SendMessage("Missing arguments. Format should be: /GetLocalInt Variable_Name. Example: /GetLocalInt MY_VARIABLE");
                return;
            }

            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid.");
                return;
            }

            string variableName = Convert.ToString(args[0]);
            int value = _.GetLocalInt(target, variableName);

           user.SendMessage(variableName + " = " + value);
        }

        public bool RequiresTarget => true;
    }
}

using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets a local float on a target.", CommandPermissionType.DM)]
    public class GetLocalFloat : IChatCommand
    {
        private readonly INWScript _;

        public GetLocalFloat(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 1)
            {
                user.SendMessage("Missing arguments. Format should be: /GetLocalFloat Variable_Name. Example: /GetLocalFloat MY_VARIABLE");
                return;
            }

            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid.");
                return;
            }

            string variableName = Convert.ToString(args[0]);
            float value = _.GetLocalFloat(target, variableName);

            user.SendMessage(variableName + " = " + value);
        }

        public bool RequiresTarget => true;
    }
}

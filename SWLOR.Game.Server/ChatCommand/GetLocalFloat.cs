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
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            string variableName = Convert.ToString(args[0]);
            float value = _.GetLocalFloat(target, variableName);

            user.SendMessage(variableName + " = " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /GetLocalFloat Variable_Name. Example: /GetLocalFloat MY_VARIABLE";
            }

            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

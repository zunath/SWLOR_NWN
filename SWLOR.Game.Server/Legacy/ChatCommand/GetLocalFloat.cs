using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Gets a local float on a target.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetLocalFloat : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            var variableName = Convert.ToString(args[0]);
            var value = NWScript.GetLocalFloat(target, variableName);

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

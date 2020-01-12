﻿using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets a local integer on a target.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetLocalInt : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsValid)
            {
                user.SendMessage("Target is invalid. Targeting area instead.");
                target = user.Area;
            }

            string variableName = Convert.ToString(args[0]);
            int value = _.GetLocalInt(target, variableName);

           user.SendMessage(variableName + " = " + value);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Missing arguments. Format should be: /GetLocalInt Variable_Name. Example: /GetLocalInt MY_VARIABLE";
            }
            
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

﻿using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using _ = SWLOR.Game.Server.NWScript._;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the world time to 8 AM.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Day: IChatCommand
    {
        /// <summary>
        /// Sets the time to 8 AM
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            _.SetTime(8, 0, 0, 0);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

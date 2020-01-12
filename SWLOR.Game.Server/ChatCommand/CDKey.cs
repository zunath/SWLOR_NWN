﻿using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using _ = SWLOR.Game.Server.NWScript._;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Displays your public CD key.", CommandPermissionType.DM | CommandPermissionType.Admin | CommandPermissionType.Player)]
    public class CDKey : IChatCommand
    {
        /// <summary>
        /// Displays the public CD key of the user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string cdKey = _.GetPCPublicCDKey(user);
            user.SendMessage("Your public CD Key is: " + cdKey);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

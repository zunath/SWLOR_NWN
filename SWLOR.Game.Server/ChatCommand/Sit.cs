﻿using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWScript.Enumerations;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Makes your character sit down.", CommandPermissionType.DM | CommandPermissionType.Admin | CommandPermissionType.Player)]
    public class Sit: IChatCommand
    {
        /// <summary>
        /// Causes user to play sitting animation.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() => _.ActionPlayAnimation(Animation.Sit_Cross, 1.0f, 9999));
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

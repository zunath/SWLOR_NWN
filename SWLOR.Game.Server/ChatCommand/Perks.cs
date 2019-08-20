﻿using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Opens the perks menu.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Perks : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            DialogService.StartConversation(user, user, "ViewPerks");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

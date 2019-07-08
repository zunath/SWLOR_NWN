using System;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns the current UTC server time.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Time : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            DateTime now = DateTime.UtcNow;
            string nowText = now.ToString("yyyy-MM-dd hh:mm:ss");

            user.SendMessage("Current Server Date: " + nowText);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
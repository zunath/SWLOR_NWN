﻿using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Enables or disables holonet chat channel.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class ToggleHolonet : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var player = DataService.Player.GetByID(user.GlobalID);
            player.DisplayHolonet = !player.DisplayHolonet;
            user.SetLocalInt("DISPLAY_HOLONET", player.DisplayHolonet ? 1 : 0);

            DataService.Set(player);

            if (player.DisplayHolonet)
            {
                user.SendMessage("Holonet chat: " + ColorTokenService.Green("ENABLED"));
            }
            else
            {
                user.SendMessage("Holonet chat: " + ColorTokenService.Red("DISABLED"));
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (!user.IsPlayer)
                return "You can only toggle the holonet on a player character.";

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

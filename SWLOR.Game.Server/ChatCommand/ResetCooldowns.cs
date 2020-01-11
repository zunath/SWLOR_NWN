﻿using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Resets a player's cooldowns.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class ResetCooldowns : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only player characters may be targeted with this command.");
                return;
            }

            var player = DataService.Player.GetByID(target.GlobalID);
            player.Cooldowns.Clear();
            DataService.Set(player);

            NWPlayer targetPlayer = target.Object;
            user.SendMessage("You have reset all of " + target.Name + "'s cooldowns.");
            targetPlayer.SendMessage("A DM has reset all of your cooldowns.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

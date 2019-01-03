﻿////////////////////////////////
/// This chat command is to toggle Plasma Cell on and off.
/// The check occurs at AbilityService.cs under HandlePlasmaCellPerk section
/// Dec. 31st, 2018 by Kenji
////////////////////////////////

using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles the Plasma Cell on and off.", CommandPermissionType.Player)]
    public class PlasmaCell : IChatCommand
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;
        public PlasmaCell(
            INWScript script,
            IColorTokenService color)
        {
            _ = script;
            _color = color;
        }
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!user.IsPlayer) return;

            //Checks if the player has Plasma Cell
            if (_.GetHasFeat((int)CustomFeatType.PlasmaCell, user) == NWScript.FALSE)
            {
                user.SendMessage(_color.Red("You do not have the perk: Plasma Cell."));
                return;
            }

            //Checks if the player has toggled plasma cell off
            if (user.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") == NWScript.FALSE)
            {
                user.SetLocalInt("PLASMA_CELL_TOGGLE_OFF", 1);
                user.SendMessage(_color.Red("Plasma Cell is now toggled off."));
                return;
            }

            //Checks if plasma cell has been toggled off
            else if (user.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") > 0)
            {
                user.DeleteLocalInt("PLASMA_CELL_TOGGLE_OFF");
                user.SendMessage(_color.Green("Plasma Cell is now toggled on!"));
                return;
            }

            //If the above aren't working, this should appear and debugging required
            else
            {
                user.SendMessage(_color.Red("Something's wrong, contact a code contributor!"));
                return;
            }
            
        }
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
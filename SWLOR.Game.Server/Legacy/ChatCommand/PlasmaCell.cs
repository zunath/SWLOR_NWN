////////////////////////////////
/// This chat command is to toggle Plasma Cell on and off.
/// The check occurs at AbilityService.cs under HandlePlasmaCellPerk section
/// Dec. 31st, 2018 by Kenji
////////////////////////////////

using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Toggles the Plasma Cell on and off.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class PlasmaCell : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!user.IsPlayer) return;

            //Checks if the player has Plasma Cell
            if (!NWScript.GetHasFeat(Feat.PlasmaCell, user))
            {
                user.SendMessage(ColorTokenService.Red("You do not have the perk: Plasma Cell."));
                return;
            }

            //Checks if the player has toggled plasma cell off
            if (user.GetLocalBool("PLASMA_CELL_TOGGLE_OFF") == false)
            {
                user.SetLocalInt("PLASMA_CELL_TOGGLE_OFF", 1);
                user.SendMessage(ColorTokenService.Red("Plasma Cell is now toggled off."));
                return;
            }

            //Checks if plasma cell has been toggled off
            else if (user.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") > 0)
            {
                user.DeleteLocalInt("PLASMA_CELL_TOGGLE_OFF");
                user.SendMessage(ColorTokenService.Green("Plasma Cell is now toggled on!"));
                return;
            }

            //If the above aren't working, this should appear and debugging required
            else
            {
                user.SendMessage(ColorTokenService.Red("Something's wrong, contact a code contributor!"));
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
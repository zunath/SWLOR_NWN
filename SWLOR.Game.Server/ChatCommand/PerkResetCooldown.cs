using System;
using System.Linq;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Resets a player's perk refund cooldowns.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class PerkResetCooldown : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only player characters may be targeted with this command.");
                return;
            }

            Player dbPlayer = DataService.Player.GetByID(target.GlobalID);
            dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
            NWPlayer targetPlayer = target.Object;
            user.SendMessage("You have reset" + target.Name + "'s refund cooldown.");
            targetPlayer.SendMessage("A DM has reset your perk refund cooldown");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
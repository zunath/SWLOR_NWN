using System;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
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

            var dbPlayer = DataService.Player.GetByID(target.GlobalID);
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
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Gets the XP bonus on a particular player.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetXPBonus : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only players may be targeted with this command.");
                return;
            }

            NWPlayer player = target.Object;
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);

            user.SendMessage(player.Name +  " XP Bonus = " + dbPlayer.XPBonus);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Enables or disables holonet chat channel.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class ToggleHolonet : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var player = DataService.Player.GetByID(user.GlobalID);
            player.DisplayHolonet = !player.DisplayHolonet;
            user.SetLocalInt("DISPLAY_HOLONET", player.DisplayHolonet ? 1 : 0);

            DataService.SubmitDataChange(player, DatabaseActionType.Update);

            if (player.DisplayHolonet)
            {
                user.SendMessage("Holonet chat: " + ColorToken.Green("ENABLED"));
            }
            else
            {
                user.SendMessage("Holonet chat: " + ColorToken.Red("DISABLED"));
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

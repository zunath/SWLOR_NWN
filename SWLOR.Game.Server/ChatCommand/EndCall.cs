using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Ends your current HoloCom call.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class EndCall : IChatCommand
    {
        // not showing in chat menu and not a valid command.
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            HoloComService.SetIsInCall(user, HoloComService.GetTargetForActiveCall(user), false);
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }
    }
}

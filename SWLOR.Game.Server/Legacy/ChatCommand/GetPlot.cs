using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Gets whether an object is marked plot.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class GetPlot : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (target.IsPlot)
            {
                user.SendMessage("Target is marked plot.");
            }
            else
            {
                user.SendMessage("Target is NOT marked plot.");
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Sets the world time to 8 PM.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Night : IChatCommand
    {
        /// <summary>
        /// Sets the world time to 8 PM.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            NWScript.SetTime(20, 0, 0, 0);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

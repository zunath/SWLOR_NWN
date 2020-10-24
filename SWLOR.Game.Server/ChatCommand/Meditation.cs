using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Makes your character meditate.", CommandPermissionType.DM | CommandPermissionType.Admin | CommandPermissionType.Player)]
    public class Meditation : IChatCommand
    {
        /// <summary>
        /// Causes user to play sitting animation.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() => _.ActionPlayAnimation(Animation.LoopingMeditate, 1.0f, 9999));
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
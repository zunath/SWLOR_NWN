using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a interact animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Interact : IChatCommand
    {
        private readonly INWScript _;

        public Interact(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_GET_MID);
            });
        }

        public bool RequiresTarget => false;
    }
}

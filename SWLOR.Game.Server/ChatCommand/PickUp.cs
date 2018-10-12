using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a pick up animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class PickUp : IChatCommand
    {
        private readonly INWScript _;

        public PickUp(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_GET_LOW);
            });
        }

        public bool RequiresTarget => false;
    }
}

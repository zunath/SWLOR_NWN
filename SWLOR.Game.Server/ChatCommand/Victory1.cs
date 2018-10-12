using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a victory 1 animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Victory1 : IChatCommand
    {
        private readonly INWScript _;

        public Victory1(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_FIREFORGET_VICTORY1);
            });
        }

        public bool RequiresTarget => false;
    }
}

using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a side-step animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class SideStep : IChatCommand
    {
        private readonly INWScript _;

        public SideStep(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, params string[] args)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE);
            });
        }
    }
}

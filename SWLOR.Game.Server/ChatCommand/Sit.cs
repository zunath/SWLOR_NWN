using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Makes your character sit down.", CommandPermissionType.DM | CommandPermissionType.Player)]
    public class Sit: IChatCommand
    {
        private readonly INWScript _;

        public Sit(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, params string[] args)
        {
            user.AssignCommand(() => _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_SIT_CROSS, 1.0f, 9999));
        }
    }
}

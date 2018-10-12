using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a taunt animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Taunt : IChatCommand
    {
        private readonly INWScript _;

        public Taunt(INWScript script)
        {
            _ = script;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_FIREFORGET_TAUNT);
            });
        }

        public bool RequiresTarget => false;
    }
}

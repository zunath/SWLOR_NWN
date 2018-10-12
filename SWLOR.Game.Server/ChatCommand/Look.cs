using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a look far animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Look : LoopingAnimationCommand
    {
        private readonly INWScript _;

        public Look(INWScript script)
        {
            _ = script;
        }

        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_LOOK_FAR, 1.0f, duration);
            });
        }
    }
}
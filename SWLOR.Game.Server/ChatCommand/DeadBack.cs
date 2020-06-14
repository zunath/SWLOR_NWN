using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a dead back animation.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class DeadBack : LoopingAnimationCommand
    {
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(Animation.LoopingDeadBack, 1.0f, duration);
            });
        }
    }
}
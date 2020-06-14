using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a listen animation.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Listen : LoopingAnimationCommand
    {
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(Animation.LoopingListen, 1.0f, duration);
            });
        }
    }
}
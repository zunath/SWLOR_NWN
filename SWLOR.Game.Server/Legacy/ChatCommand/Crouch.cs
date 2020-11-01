using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Plays a crouching animation.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Crouch : LoopingAnimationCommand
    {
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                NWScript.ActionPlayAnimation(Animation.LoopingCustom2, 1.0f, duration);
            });
        }
    }
}
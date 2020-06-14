using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a cower animation.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Cower : LoopingAnimationCommand
    {
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(Animation.LoopingCustom3, 1.0f, duration);
            });
        }
    }
}
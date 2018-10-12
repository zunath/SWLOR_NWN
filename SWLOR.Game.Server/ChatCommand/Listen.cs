using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a listen animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Listen : LoopingAnimationCommand
    {
        private readonly INWScript _;

        public Listen(INWScript script)
        {
            _ = script;
        }
        
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_LISTEN, 1.0f, duration);
            });
        }
    }
}
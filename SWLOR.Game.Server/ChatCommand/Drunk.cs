using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a drunk animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Drunk : LoopingAnimationCommand
    {
        private readonly INWScript _;

        public Drunk(INWScript script)
        {
            _ = script;
        }

        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_DRUNK, 1.0f, duration);
            });
        }
    }
}
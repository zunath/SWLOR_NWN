using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a laughing animation.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Laughing : LoopingAnimationCommand
    {
        private readonly INWScript _;

        public Laughing(INWScript script)
        {
            _ = script;
        }

        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_TALK_LAUGHING, 1.0f, duration);
            });
        }
    }
}
﻿using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Plays a tired animation.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Tired : LoopingAnimationCommand
    {
        protected override void DoAction(NWPlayer user, float duration)
        {
            user.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_TIRED, 1.0f, duration);
            });
        }
    }
}
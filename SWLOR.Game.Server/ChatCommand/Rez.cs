﻿using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Revives you, heals you to full, and restores all FP.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Rez: IChatCommand
    {
        /// <summary>
        /// Revives and heals user completely.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (user.IsDead)
            {
                _.ApplyEffectToObject(DurationType.Instant, _.EffectResurrection(), user.Object);
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectHeal(999), user.Object);
            AbilityService.RestorePlayerFP(user, 9999);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

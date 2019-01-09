﻿using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Revives you, heals you to full, and restores all FP.", CommandPermissionType.DM)]
    public class Rez: IChatCommand
    {
        private readonly INWScript _;
        private readonly IAbilityService _ability;

        public Rez(
            INWScript script,
            IAbilityService ability)
        {
            _ = script;
            _ability = ability;
        }

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
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectResurrection(), user.Object);
            }

            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(999), user.Object);
            _ability.RestoreFP(user, 9999);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}

﻿using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Kills your target.", CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Kill : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var damage = _.EffectDamage(target.MaxHP+11);
            _.ApplyEffectToObject(DurationType.Instant, damage, target);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}

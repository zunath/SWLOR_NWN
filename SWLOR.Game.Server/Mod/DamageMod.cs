﻿using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class DamageMod : IModHandler
    {
        public int ModTypeID => 25;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.DamageBonus >= 20)
                return "You cannot improve that item's damage bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.DamageBonus + value;
            if (newValue > 20) newValue = 20;
            target.DamageBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Damage +" + value;
        }
    }
}

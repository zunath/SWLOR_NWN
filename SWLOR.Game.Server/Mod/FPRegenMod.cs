﻿using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class FPRegenMod : IModHandler
    {
        public int ModTypeID => 12;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.FPRegenBonus >= 20)
                return "You cannot improve that item's FP regen bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.FPRegenBonus + value;
            if (newValue > 20) newValue = 20;
            target.FPRegenBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "FP Regen +" + value;
        }
    }
}

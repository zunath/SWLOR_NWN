﻿using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class WeaponsmithMod : IModHandler
    {
        public int ModTypeID => 18;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CraftBonusWeaponsmith >= MaxValue)
                return "You cannot improve that item's weaponsmith bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            int newValue = target.CraftBonusWeaponsmith + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CraftBonusWeaponsmith = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Weaponsmith +" + value;
        }
    }
}

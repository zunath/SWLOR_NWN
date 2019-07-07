using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class CookingMod : IModHandler
    {
        public int ModTypeID => 14;
        private const int MaxValue = 17;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CraftBonusCooking >= MaxValue)
                return "You cannot improve that item's cooking bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            int newValue = target.CraftBonusCooking + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CraftBonusCooking = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Cooking +" + value;
        }
    }
}

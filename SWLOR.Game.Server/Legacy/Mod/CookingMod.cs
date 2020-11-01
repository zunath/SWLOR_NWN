using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
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
            var amount = Convert.ToInt32(args[0]);
            var newValue = target.CraftBonusCooking + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CraftBonusCooking = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Cooking +" + value;
        }
    }
}

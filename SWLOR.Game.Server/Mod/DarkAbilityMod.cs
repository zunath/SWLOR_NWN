using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    // This is the Dark Potency Mod class. The class name is maintained for backwards compatibility purposes.
    public class DarkAbilityMod : IModHandler
    {
        public int ModTypeID => 8;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.DarkPotencyBonus >= MaxValue)
                return "You cannot improve that item's dark potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int tier = Convert.ToInt32(args[0]);
            int amount = tier * 3;
            int newValue = target.DarkPotencyBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.DarkPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Dark Potency +" + value;
        }
    }
}

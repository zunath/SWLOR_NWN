using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    // This is the Light Potency Mod class. The class name is maintained for backwards compatibility purposes.
    public class LightAbilityMod: IModHandler
    {
        public int ModTypeID => 7;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.LightPotencyBonus >= MaxValue)
                return "You cannot improve that item's Light Potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int tier = Convert.ToInt32(args[0]);
            int amount = tier * 3;
            int newValue = target.LightPotencyBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.LightPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Light Potency +" + value;
        }
    }
}

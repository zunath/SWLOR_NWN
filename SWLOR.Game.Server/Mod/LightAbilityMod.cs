using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    // This is the Force Support Mod class. The class name is maintained for backwards compatibility purposes.
    public class LightAbilityMod: IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.LightPotencyBonus >= 50)
                return "You cannot improve that item's Light Potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.LightPotencyBonus + value;
            if (newValue > 50) newValue = 50;
            target.LightPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Light Potency +" + value;
        }
    }
}

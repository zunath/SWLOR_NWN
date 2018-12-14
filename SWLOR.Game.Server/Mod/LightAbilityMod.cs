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
            if (target.ForceSupportBonus >= 50)
                return "You cannot improve that item's force support bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.ForceSupportBonus + value;
            if (newValue > 50) newValue = 50;
            target.ForceSupportBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Force Support +" + value;
        }
    }
}

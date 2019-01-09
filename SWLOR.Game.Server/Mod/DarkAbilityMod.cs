using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    // This is the Dark Potency Mod class. The class name is maintained for backwards compatibility purposes.
    public class DarkAbilityMod : IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.DarkPotencyBonus >= 50)
                return "You cannot improve that item's dark potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.DarkPotencyBonus + value;
            if (newValue > 50) newValue = 50;
            target.DarkPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Dark Potency +" + value;
        }
    }
}

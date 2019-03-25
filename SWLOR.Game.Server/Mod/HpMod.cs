using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class HPMod: IModHandler
    {
        public int ModTypeID => 5;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.HPBonus >= 100)
                return "You cannot improve that item's HP bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.HPBonus + value;
            if (newValue > 100) newValue = 100;
            target.HPBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "HP +" + value;
        }
    }
}

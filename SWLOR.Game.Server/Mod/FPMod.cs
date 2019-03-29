using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class FPMod : IModHandler
    {
        public int ModTypeID => 11;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.FPBonus >= 100)
                return "You cannot improve that item's FP bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.FPBonus + value;
            if (newValue > 100) newValue = 100;
            target.FPBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "FP +" + value;
        }
    }
}

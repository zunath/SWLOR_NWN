using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class FPMod : IModHandler
    {
        public int ModTypeID => 11;
        private const int MaxValue = 100;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.FPBonus >= MaxValue)
                return "You cannot improve that item's FP bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var amount = Convert.ToInt32(args[0]);
            var newValue = target.FPBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.FPBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "FP +" + value;
        }
    }
}

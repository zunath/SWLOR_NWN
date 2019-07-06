using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class HPMod: IModHandler
    {
        public int ModTypeID => 5;
        private const int MaxValue = 100;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.HPBonus >= MaxValue)
                return "You cannot improve that item's HP bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            int newValue = target.HPBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.HPBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "HP +" + value;
        }
    }
}

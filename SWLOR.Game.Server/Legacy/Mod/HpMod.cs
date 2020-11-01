using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
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
            var amount = Convert.ToInt32(args[0]);
            var newValue = target.HPBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.HPBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "HP +" + value;
        }
    }
}

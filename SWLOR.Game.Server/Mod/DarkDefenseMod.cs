using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class DarkDefenseMod : IModHandler
    {
        public int ModTypeID => 30;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.DarkDefenseBonus >= MaxValue)
                return "You cannot improve that item's dark defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.DarkDefenseBonus + value;
            if (newValue > MaxValue) newValue = MaxValue;
            target.DarkDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Dark Defense +" + value;
        }
    }
}

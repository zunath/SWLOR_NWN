using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class DarkDefenseMod : IModHandler
    {
        public int ModTypeID => 30;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.DarkDefenseBonus >= 20)
                return "You cannot improve that item's dark defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.DarkDefenseBonus + value;
            if (newValue > 20) newValue = 20;
            target.DarkDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Dark Defense +" + value;
        }
    }
}

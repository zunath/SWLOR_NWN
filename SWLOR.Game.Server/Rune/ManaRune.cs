using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Rune.Contracts;

namespace SWLOR.Game.Server.Rune
{
    public class ManaRune : IRune
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.ManaBonus >= 100)
                return "You cannot improve that item's mana bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.ManaBonus + value;
            if (newValue > 100) newValue = 100;
            target.ManaBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Mana +" + value;
        }
    }
}

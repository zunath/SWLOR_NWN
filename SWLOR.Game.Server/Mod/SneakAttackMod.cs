using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class SneakAttackMod: IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.SneakAttackBonus >= 20)
                return "You cannot improve that item's sneak attack bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.SneakAttackBonus + value;
            if (newValue >= 20) newValue = 20;
            target.SneakAttackBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Sneak Attack +" + value;
        }
    }
}

using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class SneakAttackMod: IModHandler
    {
        public int ModTypeID => 26;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.SneakAttackBonus >= MaxValue)
                return "You cannot improve that item's sneak attack bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.SneakAttackBonus + value;
            if (newValue >= MaxValue) newValue = MaxValue;
            target.SneakAttackBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Sneak Attack +" + value;
        }
    }
}

using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class LightDefenseMod : IModHandler
    {
        public int ModTypeID => 31;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.LightDefenseBonus >= MaxValue)
                return "You cannot improve that item's light defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int tier = Convert.ToInt32(args[0]);
            int amount = tier * 3;
            int newValue = target.LightDefenseBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.LightDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Light Defense +" + value;
        }
    }
}

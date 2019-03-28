using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class LightDefenseMod : IModHandler
    {
        public int ModTypeID => 31;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.LightDefenseBonus >= 20)
                return "You cannot improve that item's light defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.LightDefenseBonus + value;
            if (newValue > 20) newValue = 20;
            target.LightDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Light Defense +" + value;
        }
    }
}

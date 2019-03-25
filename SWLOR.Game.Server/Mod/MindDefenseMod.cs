using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class MindDefenseMod : IModHandler
    {
        public int ModTypeID => 32;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.MindDefenseBonus >= 20)
                return "You cannot improve that item's mind defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.MindDefenseBonus + value;
            if (newValue > 20) newValue = 20;
            target.MindDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Mind Defense +" + value;
        }
    }
}

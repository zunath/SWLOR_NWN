using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class HarvestingMod : IModHandler
    {
        public int ModTypeID => 16;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.HarvestingBonus >= 30)
                return "You cannot improve that item's harvesting bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.HarvestingBonus + value;
            if (newValue > 30) newValue = 30;
            target.HarvestingBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Harvesting +" + value;
        }
    }
}

using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class EngineeringMod : IModHandler
    {
        public int ModTypeID => 17;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CraftBonusEngineering >= 30)
                return "You cannot improve that item's engineering bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.CraftBonusEngineering + value;
            if (newValue > 30) newValue = 30;
            target.CraftBonusEngineering = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Engineering +" + value;
        }
    }
}

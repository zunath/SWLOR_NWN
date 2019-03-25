using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class MeditateMod : IModHandler
    {
        public int ModTypeID => 22;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.MeditateBonus >= 2)
                return "You cannot improve that item's meditate bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.MeditateBonus + value;
            if (newValue > 2) newValue = 2;
            target.MeditateBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Meditate +" + value;
        }
    }
}

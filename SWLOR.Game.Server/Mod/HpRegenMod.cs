using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class HPRegenMod: IModHandler
    {
        public int ModTypeID => 6;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.HPRegenBonus >= MaxValue)
                return "You cannot improve that item's HP regen bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.HPRegenBonus + value;
            if (newValue > MaxValue) newValue = MaxValue;
            target.HPRegenBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "HP Regen +" + value;
        }
    }
}

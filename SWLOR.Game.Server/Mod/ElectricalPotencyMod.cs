using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class ElectricalPotencyMod : IModHandler
    {
        public int ModTypeID => 29;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.ElectricalPotencyBonus >= MaxValue)
                return "You cannot improve that item's Electrical Potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.ElectricalPotencyBonus + value;
            if (newValue > MaxValue) newValue = MaxValue;
            target.ElectricalPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Electrical Potency +" + value;
        }
    }
}

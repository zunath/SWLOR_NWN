using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class MedicineMod : IModHandler
    {
        public int ModTypeID => 23;
        private const int MaxValue = 17;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.MedicineBonus >= MaxValue)
                return "You cannot improve that item's medicine bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            int newValue = target.MedicineBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.MedicineBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Medicine +" + value;
        }
    }
}

using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class MindPotencyMod : IModHandler
    {
        public int ModTypeID => 9;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.MindPotencyBonus >= MaxValue)
                return "You cannot improve that item's Mind Potency bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int tier = Convert.ToInt32(args[0]);
            int amount = tier * 3;
            int newValue = target.MindPotencyBonus + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.MindPotencyBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Mind Potency +" + value;
        }
    }
}

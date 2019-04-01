using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class ArmorsmithMod: IModHandler
    {
        public int ModTypeID => 13;
        private const int MaxValue = 51;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CraftBonusArmorsmith >= MaxValue)
                return "You cannot improve that item's armorsmith bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int tier = Convert.ToInt32(args[0]);
            int amount = tier * 3;
            int newValue = target.CraftBonusArmorsmith + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CraftBonusArmorsmith = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Armorsmith +" + value;
        }
    }
}

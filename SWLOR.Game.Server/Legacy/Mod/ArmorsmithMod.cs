using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class ArmorsmithMod: IModHandler
    {
        public int ModTypeID => 13;
        private const int MaxValue = 17;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CraftBonusArmorsmith >= MaxValue)
                return "You cannot improve that item's armorsmith bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var amount = Convert.ToInt32(args[0]);
            var newValue = target.CraftBonusArmorsmith + amount;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CraftBonusArmorsmith = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Armorsmith +" + value;
        }
    }
}

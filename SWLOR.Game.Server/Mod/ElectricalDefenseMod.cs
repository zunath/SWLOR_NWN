using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class ElectricalDefenseMod : IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.ElectricalDefenseBonus >= 20)
                return "You cannot improve that item's electrical defense bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.ElectricalDefenseBonus + value;
            if (newValue > 20) newValue = 20;
            target.ElectricalDefenseBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Electrical Defense +" + value;
        }
    }
}

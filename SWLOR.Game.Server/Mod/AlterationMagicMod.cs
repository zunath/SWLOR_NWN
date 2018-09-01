using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class AlterationMagicMod: IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.AlterationBonus >= 50)
                return "You cannot improve that item's alteration magic bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.AlterationBonus + value;
            if (newValue > 50) newValue = 50;
            target.AlterationBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Alteration Magic +" + value;
        }
    }
}

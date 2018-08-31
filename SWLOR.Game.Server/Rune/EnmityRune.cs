using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Rune.Contracts;

namespace SWLOR.Game.Server.Rune
{
    public class EnmityRune: IRune
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);

            if (value < 0)
            {
                if (target.EnmityRate <= 50)
                    return "You cannot improve that item's enmity reduction any further.";
            }
            else if (value > 0)
            {
                if (target.EnmityRate >= 50)
                    return "You cannot improve that item's enmity generation any further.";
            }

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.EnmityRate + value;
            if (newValue > 50) newValue = 50;
            else if (newValue < -50) newValue = -50;

            target.EnmityRate = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Enmity: " + value + "%";
        }
    }
}

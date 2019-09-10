using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class EnmityMod: IModHandler
    {
        public int ModTypeID => 20;
        private const int MaxValue = 50;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);

            if (value < 0)
            {
                if (target.EnmityRate <= -MaxValue)
                    return "You cannot improve that item's enmity reduction any further.";
            }
            else if (value > 0)
            {
                if (target.EnmityRate >= MaxValue)
                    return "You cannot improve that item's enmity generation any further.";
            }

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.EnmityRate + value;
            if (newValue > MaxValue) newValue = MaxValue;
            else if (newValue < -MaxValue) newValue = -MaxValue;

            target.EnmityRate = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Enmity: " + value + "%";
        }
    }
}

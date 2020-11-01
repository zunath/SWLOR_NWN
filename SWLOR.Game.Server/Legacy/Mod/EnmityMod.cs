using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class EnmityMod: IModHandler
    {
        public int ModTypeID => 20;
        private const int MaxValue = 50;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);

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
            var value = Convert.ToInt32(args[0]);
            var newValue = target.EnmityRate + value;
            if (newValue > MaxValue) newValue = MaxValue;
            else if (newValue < -MaxValue) newValue = -MaxValue;

            target.EnmityRate = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Enmity: " + value + "%";
        }
    }
}

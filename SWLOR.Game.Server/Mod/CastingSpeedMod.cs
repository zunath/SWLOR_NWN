using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class CastingSpeedMod : IModHandler
    {
        public int ModTypeID => 10;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CastingSpeed >= MaxValue)
                return "You cannot improve that item's casting speed any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.CastingSpeed + value;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CastingSpeed = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Casting Speed +" + value + "%";
        }
    }
}

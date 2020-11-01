using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class CastingSpeedMod : IModHandler
    {
        public int ModTypeID => 10;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CooldownRecovery >= MaxValue)
                return "You cannot improve that item's cooldown recovery any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            var newValue = target.CooldownRecovery + value;
            if (newValue > MaxValue) newValue = MaxValue;
            target.CooldownRecovery = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Cooldown Recovery +" + value + "%";
        }
    }
}

using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Rune.Contracts;

namespace SWLOR.Game.Server.Rune
{
    public class DurabilityRune: IRune
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.MaxDurability >= 100)
                return "You cannot improve that item's maximum durability any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            float newValue = target.MaxDurability + value;
            if (newValue > 100) newValue = 100;
            target.MaxDurability = newValue;
            target.Durability += value;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Durability +" + value;
        }
    }
}

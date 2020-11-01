using System;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class DurabilityMod: IModHandler
    {
        public int ModTypeID => 19;
        private const int MaxValue = 100;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            var maxDurability = DurabilityService.GetMaxDurability(target);
            if (maxDurability >= MaxValue)
                return "You cannot improve that item's maximum durability any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var maxDurability = DurabilityService.GetMaxDurability(target);
            var curDurability = DurabilityService.GetDurability(target);

            var value = Convert.ToInt32(args[0]);
            var newValue = maxDurability + value;
            if (newValue > MaxValue) newValue = MaxValue;
            maxDurability = newValue;
            curDurability += value;

            DurabilityService.SetMaxDurability(target, maxDurability);
            DurabilityService.SetDurability(target, curDurability);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Durability +" + value;
        }
    }
}

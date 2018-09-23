using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class DurabilityMod: IMod
    {
        private readonly IDurabilityService _durability;

        public DurabilityMod(IDurabilityService durability)
        {
            _durability = durability;
        }

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            var maxDurability = _durability.GetMaxDurability(target);
            if (maxDurability >= 100)
                return "You cannot improve that item's maximum durability any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var maxDurability = _durability.GetMaxDurability(target);
            var curDurability = _durability.GetDurability(target);

            int value = Convert.ToInt32(args[0]);
            float newValue = maxDurability + value;
            if (newValue > 100) newValue = 100;
            maxDurability = newValue;
            curDurability += value;

            _durability.SetMaxDurability(target, maxDurability);
            _durability.SetDurability(target, curDurability);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Durability +" + value;
        }
    }
}

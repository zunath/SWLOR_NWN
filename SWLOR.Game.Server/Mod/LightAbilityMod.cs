using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class LightAbilityMod: IMod
    {
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.LightAbilityBonus >= 50)
                return "You cannot improve that item's light ability bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.LightAbilityBonus + value;
            if (newValue > 50) newValue = 50;
            target.LightAbilityBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Light Ability +" + value;
        }
    }
}

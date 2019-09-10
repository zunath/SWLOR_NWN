using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Mod
{
    public class ArmorClassMod: IModHandler
    {
        public int ModTypeID => 2;
        private const int MaxValue = 17;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (target.CustomAC >= MaxValue) 
                return "You cannot improve that item's AC any further.";

            if (!ItemService.ArmorBaseItemTypes.Contains(target.BaseItemType) &&
                !ItemService.ShieldBaseItemTypes.Contains(target.BaseItemType))
                return "This mod can only be applied to armors and shields.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            int newValue = target.CustomAC + amount;
            if (newValue > MaxValue) newValue = MaxValue;

            target.CustomAC = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int amount = Convert.ToInt32(args[0]);
            return "AC +" + amount;
        }
    }
}

using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Mod
{
    public class BaseAttackBonusMod: IMod
    {
        private readonly INWScript _;
        private readonly IItemService _item;

        public BaseAttackBonusMod(
            INWScript script,
            IItemService item)
        {
            _ = script;
            _item = item;
        }
        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (!_item.ArmorBaseItemTypes.Contains(target.BaseItemType))
                return "This mod can only be applied to armors.";

            int existingBAB = target.BaseAttackBonus;
            if (existingBAB >= 10) return "You cannot improve that item's base attack bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            int newValue = target.BaseAttackBonus + value;
            if (newValue > 10) newValue = 10;
            target.BaseAttackBonus = newValue;
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Base Attack Bonus +" + value;
        }
    }
}

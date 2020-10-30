using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Mod
{
    public class AttackBonusMod: IModHandler
    {
        public int ModTypeID => 3;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (!ItemService.WeaponBaseItemTypes.Contains(target.BaseItemType))
                return "This mod can only be applied to weapons.";

            var existingAB = GetExistingAB(target);
            if (existingAB >= MaxValue) return "You cannot improve that item's attack bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var additionalAB = Convert.ToInt32(args[0]);
            var existingAB = GetExistingAB(target);
            var newValue = existingAB + additionalAB;
            if (newValue > MaxValue) newValue = MaxValue;

            var ip = NWScript.ItemPropertyAttackBonus(newValue);
            ip = NWScript.TagItemProperty(ip, "RUNE_IP");

            BiowareXP2.IPSafeAddItemProperty(target, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Attack Bonus +" + value;
        }

        private int GetExistingAB(NWItem item)
        {
            var currentAB = 0;
            foreach (var ip in item.ItemProperties)
            {
                var type = NWScript.GetItemPropertyType(ip);
                if (type == ItemPropertyType.AttackBonus)
                {
                    var bonus =  NWScript.GetItemPropertyCostTableValue(ip);
                    currentAB += bonus;
                }
            }

            return currentAB;
        }
    }
}

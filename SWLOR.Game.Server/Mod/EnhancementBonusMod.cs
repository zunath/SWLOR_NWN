using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Mod.Contracts;

using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Mod
{
    public class EnhancementBonusMod : IModHandler
    {
        public int ModTypeID => 24;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (!ItemService.WeaponBaseItemTypes.Contains(target.BaseItemType))
                return "This mod can only be applied to weapons.";

            int existingEnhancementBonus = GetExistingEnhancementBonus(target);
            if (existingEnhancementBonus >= MaxValue) return "You cannot improve that item's enhancement bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int additionalEnhancementBonus = Convert.ToInt32(args[0]);
            int existingEnhancementBonus = GetExistingEnhancementBonus(target);
            int newValue = existingEnhancementBonus + additionalEnhancementBonus;
            if (newValue > MaxValue) newValue = MaxValue;

            ItemProperty ip = _.ItemPropertyEnhancementBonus(newValue);
            ip = _.TagItemProperty(ip, "RUNE_IP");

            BiowareXP2.IPSafeAddItemProperty(target, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            int value = Convert.ToInt32(args[0]);
            return "Enhancement Bonus +" + value;
        }

        private int GetExistingEnhancementBonus(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                if (type == _.ITEM_PROPERTY_ENHANCEMENT_BONUS)
                {
                    return _.GetItemPropertyCostTableValue(ip);
                }
            }

            return 0;
        }
    }
}

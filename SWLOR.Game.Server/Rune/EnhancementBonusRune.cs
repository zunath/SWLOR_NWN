using System;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Rune.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Rune
{
    public class EnhancementBonusRune : IRune
    {
        private readonly INWScript _;
        private readonly IItemService _item;
        private readonly IBiowareXP2 _biowareXP2;

        public EnhancementBonusRune(
            INWScript script,
            IItemService item,
            IBiowareXP2 biowareXP2)
        {
            _ = script;
            _item = item;
            _biowareXP2 = biowareXP2;
        }

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (!_item.WeaponBaseItemTypes.Contains(target.BaseItemType))
                return "This rune can only be applied to weapons.";

            int existingEnhancementBonus = GetExistingEnhancementBonus(target);
            if (existingEnhancementBonus >= 20) return "You cannot improve that item's enhancement bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            int additionalEnhancementBonus = Convert.ToInt32(args[0]);
            int existingEnhancementBonus = GetExistingEnhancementBonus(target);
            int newValue = existingEnhancementBonus + additionalEnhancementBonus;
            if (newValue > 20) newValue = 20;

            ItemProperty ip = _.ItemPropertyEnhancementBonus(newValue);
            ip = _.TagItemProperty(ip, "RUNE_IP");

            _biowareXP2.IPSafeAddItemProperty(target, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
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
                if (type == NWScript.ITEM_PROPERTY_ENHANCEMENT_BONUS)
                {
                    return _.GetItemPropertyCostTableValue(ip);
                }
            }

            return 0;
        }
    }
}

using System;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Mod.Contracts;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Mod
{
    public class EnhancementBonusMod : IModHandler
    {
        public int ModTypeID => 24;
        private const int MaxValue = 20;

        public string CanApply(NWPlayer player, NWItem target, params string[] args)
        {
            if (!ItemService.WeaponBaseItemTypes.Contains(target.BaseItemType))
                return "This mod can only be applied to weapons.";

            var existingEnhancementBonus = GetExistingEnhancementBonus(target);
            if (existingEnhancementBonus >= MaxValue) return "You cannot improve that item's enhancement bonus any further.";

            return null;
        }

        public void Apply(NWPlayer player, NWItem target, params string[] args)
        {
            var additionalEnhancementBonus = Convert.ToInt32(args[0]);
            var existingEnhancementBonus = GetExistingEnhancementBonus(target);
            var newValue = existingEnhancementBonus + additionalEnhancementBonus;
            if (newValue > MaxValue) newValue = MaxValue;

            var ip = NWScript.ItemPropertyEnhancementBonus(newValue);
            ip = NWScript.TagItemProperty(ip, "RUNE_IP");

            BiowareXP2.IPSafeAddItemProperty(target, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

        public string Description(NWPlayer player, NWItem target, params string[] args)
        {
            var value = Convert.ToInt32(args[0]);
            return "Enhancement Bonus +" + value;
        }

        private int GetExistingEnhancementBonus(NWItem item)
        {
            foreach (var ip in item.ItemProperties)
            {
                var type = NWScript.GetItemPropertyType(ip);
                if (type == ItemPropertyType.EnhancementBonus)
                {
                    return NWScript.GetItemPropertyCostTableValue(ip);
                }
            }

            return 0;
        }
    }
}

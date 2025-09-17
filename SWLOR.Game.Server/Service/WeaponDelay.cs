using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    public static class WeaponDelay
    {
        /// <summary>
        /// Dictionary mapping weapon category lists to their delay values
        /// </summary>
        private static readonly Dictionary<List<BaseItem>, int> _weaponCategoryDelays = new()
        {
            {Item.VibrobladeBaseItemTypes, 23},
            {Item.KatarBaseItemTypes, 21},
            {Item.TwinBladeBaseItemTypes, 42},
            {Item.FinesseVibrobladeBaseItemTypes, 20},
            {Item.StaffBaseItemTypes, 37},
            {Item.RifleBaseItemTypes, 45},
            {Item.HeavyVibrobladeBaseItemTypes, 45},
            {Item.PistolBaseItemTypes, 29},
            {Item.LightsaberBaseItemTypes, 24},
            {Item.PolearmBaseItemTypes, 40},
            {Item.ThrowingWeaponBaseItemTypes, 18},
            {Item.SaberstaffBaseItemTypes, 43}
        };

        /// <summary>
        /// Gets the delay value for a weapon by its BaseItemType
        /// </summary>
        /// <param name="baseItemType">The BaseItemType of the weapon</param>
        /// <returns>The delay value, or null if not found</returns>
        public static int? GetWeaponDelay(BaseItem baseItemType)
        {
            foreach (var category in _weaponCategoryDelays.Keys)
            {
                if (category.Contains(baseItemType))
                {
                    return _weaponCategoryDelays[category];
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if a weapon has a Delay item property
        /// </summary>
        /// <param name="item">The weapon item to check</param>
        /// <returns>True if the weapon has a Delay property, false otherwise</returns>
        public static bool HasDelayProperty(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a Delay item property to a weapon
        /// </summary>
        /// <param name="item">The weapon item to add the property to</param>
        /// <param name="delayValue">The delay value to set (cost table value)</param>
        public static void AddDelayProperty(uint item, int delayValue)
        {
            var delayProperty = ItemPropertyCustom(ItemPropertyType.Delay, -1, delayValue);
            BiowareXP2.IPSafeAddItemProperty(item, delayProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
        }

        /// <summary>
        /// Checks if an item is a weapon that should have delay properties
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if the item is a weapon that should have delay properties</returns>
        public static bool IsWeapon(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            return Item.WeaponBaseItemTypes.Contains(baseItemType);
        }

        /// <summary>
        /// Applies delay property to a weapon if it's missing and the weapon has a known delay value
        /// </summary>
        /// <param name="item">The weapon item to process</param>
        public static void ApplyDelayPropertyIfMissing(uint item)
        {
            if (!IsWeapon(item))
            {
                return;
            }

            if (HasDelayProperty(item))
            {
                return;
            }

            var baseItemType = GetBaseItemType(item);
            var delayValue = GetWeaponDelay(baseItemType);

            if (delayValue.HasValue)
            {
                AddDelayProperty(item, delayValue.Value);
            }
        }

        /// <summary>
        /// When a weapon is equipped, check if it has a Delay item property.
        /// If not, and it's a known weapon type, add the appropriate delay property.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void ApplyWeaponDelayFallback()
        {
            var item = GetPCItemLastEquipped();
            if (!GetIsObjectValid(item)) 
                return;

            ApplyDelayPropertyIfMissing(item);
        }
    }
}

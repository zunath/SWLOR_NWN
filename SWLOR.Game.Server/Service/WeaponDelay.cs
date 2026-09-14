using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    public static class WeaponDelay
    {
        /// <summary>
        /// Dictionary mapping weapon category lists to their delay values
        /// </summary>
        private static readonly Dictionary<List<BaseItem>, ItemPropertyAttackDelay> _weaponCategoryDelays = new()
        {
            {Item.VibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay230},
            {Item.KatarBaseItemTypes, ItemPropertyAttackDelay.Delay220},
            {Item.TwinBladeBaseItemTypes, ItemPropertyAttackDelay.Delay290},
            {Item.VibroknifeBaseItemTypes, ItemPropertyAttackDelay.Delay220},
            {Item.StaffBaseItemTypes, ItemPropertyAttackDelay.Delay270},
            {Item.RifleBaseItemTypes, ItemPropertyAttackDelay.Delay300},
            {Item.HeavyVibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay300},
            {Item.PistolBaseItemTypes, ItemPropertyAttackDelay.Delay250},
            {Item.LightsaberBaseItemTypes, ItemPropertyAttackDelay.Delay240},
            {Item.SpearBaseItemTypes, ItemPropertyAttackDelay.Delay280},
            {Item.ThrowingWeaponBaseItemTypes, ItemPropertyAttackDelay.Delay220},
            {Item.SaberstaffBaseItemTypes, ItemPropertyAttackDelay.Delay290},
            {Item.CreatureBaseItemTypes, ItemPropertyAttackDelay.Delay240}
        };

        /// <summary>
        /// Gets the delay value for a weapon by its BaseItemType
        /// </summary>
        /// <param name="baseItemType">The BaseItemType of the weapon</param>
        /// <returns>The delay value, or null if not found</returns>
        public static ItemPropertyAttackDelay? GetWeaponDelay(BaseItem baseItemType)
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
        /// <param name="delay">The delay value to set.</param>
        public static void AddDelayProperty(uint item, ItemPropertyAttackDelay delay)
        {
            if (delay == ItemPropertyAttackDelay.Invalid || !Enum.IsDefined(delay))
                throw new ArgumentOutOfRangeException(nameof(delay), delay, "Weapon attack delay must be a valid iprp_delay.2da value.");

            var delayProperty = ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)delay);
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
            return GetWeaponDelay(baseItemType).HasValue;
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
            var delay = GetWeaponDelay(baseItemType);

            if (delay.HasValue)
            {
                AddDelayProperty(item, delay.Value);
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

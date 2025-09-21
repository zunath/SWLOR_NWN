using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IItemService
    {
        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        void CacheData();

        void Load2DACache();
        void LoadItemToDamageStatMapping();
        void LoadItemToAccuracyStatMapping();

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of damage calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        AbilityType GetWeaponDamageAbilityType(BaseItem itemType);

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of accuracy calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        AbilityType GetWeaponAccuracyAbilityType(BaseItem itemType);

        /// <summary>
        /// When an item is used, if its tag is in the item cache, run it through the action item process.
        /// </summary>
        void UseItem();

        /// <summary>
        /// Checks all of the "Use Limitation: Perk" item properties on an item against a creature's effective level in the required perk.
        /// If player meets or exceeds the level required for all item properties, returns true. Otherwise returns false.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to pull requirements from.</param>
        /// <returns>true if all requirements met, false otherwise</returns>
        bool CanCreatureUseItem(uint creature, uint item);

        /// <summary>
        /// Returns an item to a target.
        /// </summary>
        /// <param name="target">The target receiving the item.</param>
        /// <param name="item">The item being returned.</param>
        void ReturnItem(uint target, uint item);

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        int GetInventoryItemCount(uint obj);

        /// <summary>
        /// Retrieves the armor type of an item.
        /// This is based on the Use Limitation: Perk property.
        /// If it's not specified, ArmorType.Invalid will be returned.
        /// </summary>
        /// <param name="item">The item to be checked.</param>
        /// <returns>The ArmorType value of the item. Returns ArmorType.Invalid if neither Light or Heavy are found.</returns>
        ArmorType GetArmorType(uint item);

        /// <summary>
        /// Retrieves the list of weapon base item types.
        /// </summary>
        List<BaseItem> WeaponBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of armor base item types.
        /// </summary>
        List<BaseItem> ArmorBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of shield base item types.
        /// </summary>
        List<BaseItem> ShieldBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        List<BaseItem> VibrobladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Finesse Vibroblade base item types.
        /// </summary>
        List<BaseItem> FinesseVibrobladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        List<BaseItem> LightsaberBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        List<BaseItem> HeavyVibrobladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Polearm base item types.
        /// </summary>
        List<BaseItem> PolearmBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        List<BaseItem> TwinBladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        List<BaseItem> SaberstaffBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        List<BaseItem> KatarBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        List<BaseItem> StaffBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        List<BaseItem> PistolBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Throwing Weapon base item types.
        /// </summary>
        List<BaseItem> ThrowingWeaponBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        List<BaseItem> RifleBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of One-Handed weapon types.
        /// These are the weapons which are held in one hand and not necessarily associated with the One-Handed skill.
        /// </summary>
        List<BaseItem> OneHandedMeleeItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Two-Handed melee weapon types.
        /// These are the weapons which are held in two hand and not necessarily associated with the Two-Handed skill.
        /// </summary>
        List<BaseItem> TwoHandedMeleeItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Creature base item types.
        /// </summary>
        List<BaseItem> CreatureBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Droid base item types.
        /// These are items which require the Use Limitation Race: Droid item property in order to be equipped by a Droid.
        /// </summary>
        List<BaseItem> DroidBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the icon used on the UIs. 
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        string GetIconResref(uint item);

        /// <summary>
        /// Builds a string containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A string containing all of the item properties.</returns>
        string BuildItemPropertyString(uint item);

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        GuiBindingList<string> BuildItemPropertyList(uint item);

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an i tem.
        /// </summary>
        /// <param name="itemProperties">The list of item properties to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        GuiBindingList<string> BuildItemPropertyList(List<ItemProperty> itemProperties);

        /// <summary>
        /// Determines whether an item can be stored persistently in the database.
        /// </summary>
        /// <param name="player">The player attempting to persistently store the item.</param>
        /// <param name="item">The item being stored.</param>
        /// <returns>An error message if validation fails, otherwise an empty string if it succeeds.</returns>
        string CanBePersistentlyStored(uint player, uint item);

        /// <summary>
        /// Returns the cumulative DMG value on a given item.
        /// A minimum of 1 is always returned.
        /// No checks for item type are made in this method.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>The DMG rating, or 1 if not found.</returns>
        int GetDMG(uint item);

        /// <summary>
        /// Retrieves the critical modifier for a given item type.
        /// The value returned is based on the baseitems.2da file.
        /// </summary>
        /// <param name="type">The item type to check</param>
        /// <returns>The critical modifer value.</returns>
        int GetCriticalModifier(BaseItem type);

        /// <summary>
        /// Reduces an item stack by a specific amount.
        /// If there are not enough items in the stack to reduce, false will be returned.
        /// If the stack size of the item will reach 0, the item is destroyed and true will be returned.
        /// If the stack size will reach a number greater than 0, the item's stack size will be updated and true will be returned.
        /// </summary>
        /// <param name="item">The item to adjust</param>
        /// <param name="reduceBy">The amount to reduce by. Absolute value is used to determine this value.</param>
        /// <returns>true if successfully reduced or destroyed, false otherwise</returns>
        bool ReduceItemStack(uint item, int reduceBy);

        /// <summary>
        /// Determines if an item is a legacy item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if item is legacy, false otherwise</returns>
        bool IsLegacyItem(uint item);

        /// <summary>
        /// Marks an item as a legacy item.
        /// </summary>
        /// <param name="item">The item to mark as legacy.</param>
        void MarkLegacyItem(uint item);

        /// <summary>
        /// Retrieves the item slot of a specific item.
        /// If the item isn't equipped, InventorySlot.Invalid will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The inventory slot of the item or InventorySlot.Invalid if not equipped.</returns>
        InventorySlot GetItemSlot(uint creature, uint item);
    }
}
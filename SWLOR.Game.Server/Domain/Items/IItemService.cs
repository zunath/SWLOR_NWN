using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Items: the registered item use actions, equip and use limits, inventory checks, item property text, icons and the
    /// base item type groups. <c>Service.Item</c> is the static facade and keeps the event handlers; every member of it
    /// forwards to this interface.
    /// </summary>
    public interface IItemService
    {
        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        void CacheData();

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
        /// Ensure saved and newly acquired scripted items expose their declared activation action.
        /// </summary>
        void EnsureAcquiredItemActivation();

        /// <summary>
        /// When a player enters the module, repairs the activation action of every scripted item in their inventory and in the bags they carry.
        /// </summary>
        void EnsureInventoryItemActivations();

        /// <summary>
        /// When a registered item is used, run its scripted action instead of native behavior.
        /// </summary>
        void UseItem();

        /// <summary>
        /// Checks all item use limitation properties against a creature's effective requirements.
        /// </summary>
        bool CanCreatureUseItem(uint creature, uint item);

        /// <summary>
        /// The reason a creature may not use an item (the first unmet perk, skill or stat limitation), or an empty string when it may. Pass SkillType.TwinPistol to check a paired main-hand pistol against the Twin Pistol rank.
        /// </summary>
        string GetCreatureItemUseError(uint creature, uint item, SkillType weaponSkillRequirementOverride = SkillType.Invalid);

        /// <summary>
        /// Why a creature cannot equip an item (pair rules, the Force Sensitive limit, the crafting window, use limits, racial limits), or an empty string when it can. The prospective slot is the slot the item is about to go into.
        /// </summary>
        string CanEquip(uint creature, uint item, InventorySlot? prospectiveSlot = null);

        /// <summary>
        /// Checks the Droid-only and no-Droid equip limits of an item.
        /// </summary>
        string GetRacialEquipError(uint creature, uint item);

        /// <summary>
        /// True when the creature may equip Force Sensitive weapons: DMs and non-player creatures may, droids and beasts may not, and a player must be Force Sensitive.
        /// </summary>
        bool CanEquipForceSensitiveWeapon(uint creature);

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
        /// True when the engine finds room for the item in the creature's own inventory grid. Acquiring an item that
        /// has no room can remove items the creature already carries, so check this before ObjectPlugin.AcquireItem.
        /// A matching carried stack does not count as room: with no free cell the engine removes that stack instead of
        /// merging into it.
        /// </summary>
        bool FitsInInventory(uint creature, uint item);

        /// <summary>
        /// Moves an item that nothing holds, such as a deserialized copy, into the creature's inventory when it has room.
        /// Otherwise destroys the item and returns false, so the caller keeps the record it came from.
        /// </summary>
        bool TryAcquireItem(uint creature, uint item);

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
        /// True when the item is valid and its base item type is one of the given types.
        /// </summary>
        bool IsBaseItemType(uint item, IReadOnlyCollection<BaseItem> baseItemTypes);

        /// <summary>Includes every weapon profile read by combat, including natural weapons and unarmed gear.</summary>
        bool IsAttackWeaponType(BaseItem baseItem);

        /// <summary>Weapons whose two striking ends share one equipped item.</summary>
        bool IsDoubleWeaponType(BaseItem baseItem);

        /// <summary>
        /// True when the native item exists and its base item type is one of the given types.
        /// </summary>
        bool IsBaseItemType(global::NWN.Native.API.CNWSItem item, IReadOnlyCollection<BaseItem> baseItemTypes);

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        List<BaseItem> VibrobladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Vibroknife base item types.
        /// </summary>
        List<BaseItem> VibroknifeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        List<BaseItem> LightsaberBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        List<BaseItem> HeavyVibrobladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Spear base item types.
        /// </summary>
        List<BaseItem> SpearBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        List<BaseItem> TwinBladeBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        List<BaseItem> SaberstaffBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of base item types restricted to Force Sensitive characters.
        /// </summary>
        List<BaseItem> ForceSensitiveWeaponBaseItemTypes { get; }

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        List<BaseItem> KatarBaseItemTypes { get; }

        /// <summary>Equipped hand-to-hand weapons. Armor-slot gloves remain armor.</summary>
        List<BaseItem> PowerGloveBaseItemTypes { get; }

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
        /// Retrieves the list of one-hand melee weapon base item types.
        /// These are physical equip categories, not skill categories.
        /// </summary>
        List<BaseItem> OneHandedMeleeItemTypes { get; }

        /// <summary>
        /// Retrieves the list of two-handed melee weapon base item types.
        /// These are physical equip categories, not skill categories.
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
        /// Retrieves the icon used on the UIs. Every returned resref is verified to exist as a
        /// texture; unresolvable icons fall back to the base item's default icon and finally to
        /// a generic icon so NUI never renders a red missing-texture X.
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        string GetIconResref(uint item);

        /// <summary>
        /// Retrieves the icon layers to draw on top of each other, bottom first. Three-part weapons such as
        /// lightsabers have one icon per model part, and their base icon shows only the bottom part; every other
        /// item has a single layer, the same as <see cref="GetIconResref"/>.
        /// </summary>
        /// <param name="item">The item to retrieve the icon layers for.</param>
        /// <returns>The bottom, middle and top icons of a three-part weapon, otherwise the single icon.</returns>
        string[] GetIconLayers(uint item);

        /// <summary>
        /// Determines whether an item has a real inventory icon, as opposed to falling back to the
        /// generic placeholder icon. Items with no real icon are almost always internal, prop, or
        /// creature items that should not appear on player-facing economy surfaces.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if a real icon resource resolved, false if the generic fallback was used.</returns>
        bool HasInventoryIcon(uint item);

        /// <summary>
        /// Determines whether an item should be hidden from player-facing economy surfaces (contract
        /// objective search, and any future market-style blueprint pickers). Combines creature base
        /// types, the reserved NPC name prefixes, an explicit blueprint opt-out flag, and the absence
        /// of a real inventory icon. This is the single source of truth; callers must not re-derive it.
        /// </summary>
        /// <param name="item">The item to classify.</param>
        /// <returns>true if the item is NPC/creature/internal and should not be shown to players.</returns>
        bool IsEconomyRestricted(uint item);

        /// <summary>
        /// Data-only form of the shared economy classifier. Builder tools use this overload while
        /// inspecting UTI data that has not been instantiated by the game engine.
        /// </summary>
        bool IsEconomyRestricted( BaseItem baseItem, string name, bool noEconomy, bool hasInventoryIcon);

        /// <summary>
        /// The name-based portion of <see cref="IsEconomyRestricted"/>, split out so it can be unit
        /// tested without spawning an item. A blank name denotes an internal/unfinished blueprint.
        /// </summary>
        /// <param name="name">The item's display name.</param>
        /// <returns>true if the name marks the item as NPC-only or internal.</returns>
        bool IsEconomyRestrictedName(string name);

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
        /// Returns true if <see cref="PlayerProducedItemVariable"/> is set (e.g. crafting stamped the item).
        /// </summary>
        bool IsPlayerProducedItem(uint item);

        /// <summary>
        /// Determines if an item is a legacy item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if item is legacy, false otherwise</returns>
        bool IsLegacyItem(uint item);

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

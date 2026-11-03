using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of items: the registered item use actions, equip and use limits, inventory checks, item property
    /// text, icons and the base item type groups. The logic lives in <see cref="IItemService"/>; every member here
    /// forwards to the container's instance.
    /// </summary>
    public static class Item
    {
        private static IItemService Instance => FacadeAccessor.Resolve<IItemService>();

        /// <summary>
        /// NWN local bool: set to true on crafted output so collect objectives can require player-made items.
        /// </summary>
        public const string PlayerProducedItemVariable = Domain.Items.ItemService.PlayerProducedItemVariable;

        /// <summary>
        /// Blueprint local variable that explicitly excludes an item from player-facing economy surfaces.
        /// Set this on NPC-only blueprints that a normal player item is otherwise indistinguishable from
        /// (a real base type, a real icon, and no [NPC] name), such as the "Specialist" NPC weapons.
        /// </summary>
        public const string NoEconomyVariable = Domain.Items.ItemService.NoEconomyVariable;

        /// <summary>The message shown when an item cannot be returned because the player's inventory is full.</summary>
        public const string InventoryFullMessage = "Your inventory is full. Make room and try again.";

        /// <summary>The inventory-full message for window labels too narrow for the full one.</summary>
        public const string InventoryFullShortMessage = "Your inventory is full.";

        /// <summary>The message shown when a closing window keeps the items that did not fit until it is opened again.</summary>
        public const string ItemsHeldMessage = "Your inventory is full. Items that did not fit stay at the station; open it again after making room.";

        /// <summary>
        /// When the module loads, all item details are loaded into the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of damage calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        public static AbilityType GetWeaponDamageAbilityType(BaseItem itemType)
        {
            return Instance.GetWeaponDamageAbilityType(itemType);
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of accuracy calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="itemType">The item type</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        public static AbilityType GetWeaponAccuracyAbilityType(BaseItem itemType)
        {
            return Instance.GetWeaponAccuracyAbilityType(itemType);
        }

        /// <summary>
        /// Ensure saved and newly acquired scripted items expose their declared activation action.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void EnsureAcquiredItemActivation()
        {
            Instance.EnsureAcquiredItemActivation();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void EnsureInventoryItemActivations()
        {
            Instance.EnsureInventoryItemActivations();
        }

        /// <summary>
        /// When a registered item is used, run its scripted action instead of native behavior.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUseBefore)]
        public static void UseItem()
        {
            Instance.UseItem();
        }

        /// <summary>
        /// Checks all item use limitation properties against a creature's effective requirements.
        /// </summary>
        public static bool CanCreatureUseItem(uint creature, uint item)
        {
            return Instance.CanCreatureUseItem(creature, item);
        }

        public static string GetCreatureItemUseError(uint creature, uint item, SkillType weaponSkillRequirementOverride = SkillType.Invalid)
        {
            return Instance.GetCreatureItemUseError(creature, item, weaponSkillRequirementOverride);
        }

        public static string CanEquip(uint creature, uint item, InventorySlot? prospectiveSlot = null)
        {
            return Instance.CanEquip(creature, item, prospectiveSlot);
        }

        /// <summary>
        /// Checks the Droid-only and no-Droid equip limits of an item.
        /// </summary>
        public static string GetRacialEquipError(uint creature, uint item)
        {
            return Instance.GetRacialEquipError(creature, item);
        }

        public static bool CanEquipForceSensitiveWeapon(uint creature)
        {
            return Instance.CanEquipForceSensitiveWeapon(creature);
        }

        /// <summary>
        /// Returns an item to a target.
        /// </summary>
        /// <param name="target">The target receiving the item.</param>
        /// <param name="item">The item being returned.</param>
        public static void ReturnItem(uint target, uint item)
        {
            Instance.ReturnItem(target, item);
        }

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        public static int GetInventoryItemCount(uint obj)
        {
            return Instance.GetInventoryItemCount(obj);
        }

        /// <summary>
        /// True when the engine finds room for the item in the creature's own inventory grid. Acquiring an item that
        /// has no room can remove items the creature already carries, so check this before ObjectPlugin.AcquireItem.
        /// A matching carried stack does not count as room: with no free cell the engine removes that stack instead of
        /// merging into it.
        /// </summary>
        public static bool FitsInInventory(uint creature, uint item)
        {
            return Instance.FitsInInventory(creature, item);
        }

        /// <summary>
        /// Moves an item that nothing holds, such as a deserialized copy, into the creature's inventory when it has room.
        /// Otherwise destroys the item and returns false, so the caller keeps the record it came from.
        /// </summary>
        public static bool TryAcquireItem(uint creature, uint item)
        {
            return Instance.TryAcquireItem(creature, item);
        }

        /// <summary>
        /// Retrieves the list of weapon base item types.
        /// </summary>
        public static List<BaseItem> WeaponBaseItemTypes => Instance.WeaponBaseItemTypes;

        /// <summary>
        /// Retrieves the list of armor base item types.
        /// </summary>
        public static List<BaseItem> ArmorBaseItemTypes => Instance.ArmorBaseItemTypes;

        /// <summary>
        /// Retrieves the list of shield base item types.
        /// </summary>
        public static List<BaseItem> ShieldBaseItemTypes => Instance.ShieldBaseItemTypes;

        public static bool IsBaseItemType(uint item, IReadOnlyCollection<BaseItem> baseItemTypes)
        {
            return Instance.IsBaseItemType(item, baseItemTypes);
        }

        /// <summary>Includes every weapon profile read by combat, including natural weapons and unarmed gear.</summary>
        public static bool IsAttackWeaponType(BaseItem baseItem)
        {
            return Instance.IsAttackWeaponType(baseItem);
        }

        /// <summary>Weapons whose two striking ends share one equipped item.</summary>
        public static bool IsDoubleWeaponType(BaseItem baseItem)
        {
            return Instance.IsDoubleWeaponType(baseItem);
        }

        public static bool IsBaseItemType(global::NWN.Native.API.CNWSItem item, IReadOnlyCollection<BaseItem> baseItemTypes)
        {
            return Instance.IsBaseItemType(item, baseItemTypes);
        }

        /// <summary>
        /// Retrieves the list of Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> VibrobladeBaseItemTypes => Instance.VibrobladeBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Vibroknife base item types.
        /// </summary>
        public static List<BaseItem> VibroknifeBaseItemTypes => Instance.VibroknifeBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Lightsaber base item types.
        /// </summary>
        public static List<BaseItem> LightsaberBaseItemTypes => Instance.LightsaberBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Heavy Vibroblade base item types.
        /// </summary>
        public static List<BaseItem> HeavyVibrobladeBaseItemTypes => Instance.HeavyVibrobladeBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Spear base item types.
        /// </summary>
        public static List<BaseItem> SpearBaseItemTypes => Instance.SpearBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Twin Blade base item types.
        /// </summary>
        public static List<BaseItem> TwinBladeBaseItemTypes => Instance.TwinBladeBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Saberstaff base item types.
        /// </summary>
        public static List<BaseItem> SaberstaffBaseItemTypes => Instance.SaberstaffBaseItemTypes;

        /// <summary>
        /// Retrieves the list of base item types restricted to Force Sensitive characters.
        /// </summary>
        public static List<BaseItem> ForceSensitiveWeaponBaseItemTypes => Instance.ForceSensitiveWeaponBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Katar base item types.
        /// </summary>
        public static List<BaseItem> KatarBaseItemTypes => Instance.KatarBaseItemTypes;

        /// <summary>Equipped hand-to-hand weapons. Armor-slot gloves remain armor.</summary>
        public static List<BaseItem> PowerGloveBaseItemTypes => Instance.PowerGloveBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Staff base item types.
        /// </summary>
        public static List<BaseItem> StaffBaseItemTypes => Instance.StaffBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Pistol base item types.
        /// </summary>
        public static List<BaseItem> PistolBaseItemTypes => Instance.PistolBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Throwing Weapon base item types.
        /// </summary>
        public static List<BaseItem> ThrowingWeaponBaseItemTypes => Instance.ThrowingWeaponBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Rifle base item types.
        /// </summary>
        public static List<BaseItem> RifleBaseItemTypes => Instance.RifleBaseItemTypes;

        /// <summary>
        /// Retrieves the list of one-hand melee weapon base item types.
        /// These are physical equip categories, not skill categories.
        /// </summary>
        public static List<BaseItem> OneHandedMeleeItemTypes => Instance.OneHandedMeleeItemTypes;

        /// <summary>
        /// Retrieves the list of two-handed melee weapon base item types.
        /// These are physical equip categories, not skill categories.
        /// </summary>
        public static List<BaseItem> TwoHandedMeleeItemTypes => Instance.TwoHandedMeleeItemTypes;

        /// <summary>
        /// Retrieves the list of Creature base item types.
        /// </summary>
        public static List<BaseItem> CreatureBaseItemTypes => Instance.CreatureBaseItemTypes;

        /// <summary>
        /// Retrieves the list of Droid base item types.
        /// These are items which require the Use Limitation Race: Droid item property in order to be equipped by a Droid.
        /// </summary>
        public static List<BaseItem> DroidBaseItemTypes => Instance.DroidBaseItemTypes;

        /// <summary>
        /// Retrieves the icon used on the UIs. Every returned resref is verified to exist as a
        /// texture; unresolvable icons fall back to the base item's default icon and finally to
        /// a generic icon so NUI never renders a red missing-texture X.
        /// </summary>
        /// <param name="item">The item to retrieve the icon for.</param>
        /// <returns>A resref of the icon to use.</returns>
        public static string GetIconResref(uint item)
        {
            return Instance.GetIconResref(item);
        }

        /// <summary>
        /// Retrieves the icon layers to draw on top of each other, bottom first. Three-part weapons such as
        /// lightsabers have one icon per model part, and their base icon shows only the bottom part; every other
        /// item has a single layer, the same as <see cref="GetIconResref"/>.
        /// </summary>
        /// <param name="item">The item to retrieve the icon layers for.</param>
        /// <returns>The bottom, middle and top icons of a three-part weapon, otherwise the single icon.</returns>
        public static string[] GetIconLayers(uint item)
        {
            return Instance.GetIconLayers(item);
        }

        /// <summary>
        /// Determines whether an item has a real inventory icon, as opposed to falling back to the
        /// generic placeholder icon. Items with no real icon are almost always internal, prop, or
        /// creature items that should not appear on player-facing economy surfaces.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if a real icon resource resolved, false if the generic fallback was used.</returns>
        public static bool HasInventoryIcon(uint item)
        {
            return Instance.HasInventoryIcon(item);
        }

        /// <summary>
        /// Determines whether an item should be hidden from player-facing economy surfaces (contract
        /// objective search, and any future market-style blueprint pickers). Combines creature base
        /// types, the reserved NPC name prefixes, an explicit blueprint opt-out flag, and the absence
        /// of a real inventory icon. This is the single source of truth; callers must not re-derive it.
        /// </summary>
        /// <param name="item">The item to classify.</param>
        /// <returns>true if the item is NPC/creature/internal and should not be shown to players.</returns>
        public static bool IsEconomyRestricted(uint item)
        {
            return Instance.IsEconomyRestricted(item);
        }

        /// <summary>
        /// Data-only form of the shared economy classifier. Builder tools use this overload while
        /// inspecting UTI data that has not been instantiated by the game engine.
        /// </summary>
        public static bool IsEconomyRestricted( BaseItem baseItem, string name, bool noEconomy, bool hasInventoryIcon)
        {
            return Instance.IsEconomyRestricted(baseItem, name, noEconomy, hasInventoryIcon);
        }

        /// <summary>
        /// The name-based portion of <see cref="IsEconomyRestricted"/>, split out so it can be unit
        /// tested without spawning an item. A blank name denotes an internal/unfinished blueprint.
        /// </summary>
        /// <param name="name">The item's display name.</param>
        /// <returns>true if the name marks the item as NPC-only or internal.</returns>
        public static bool IsEconomyRestrictedName(string name)
        {
            return Instance.IsEconomyRestrictedName(name);
        }

        /// <summary>
        /// Builds a string containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A string containing all of the item properties.</returns>
        public static string BuildItemPropertyString(uint item)
        {
            return Instance.BuildItemPropertyString(item);
        }

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an item.
        /// </summary>
        /// <param name="item">The item to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        public static GuiBindingList<string> BuildItemPropertyList(uint item)
        {
            return Instance.BuildItemPropertyList(item);
        }

        /// <summary>
        /// Builds a list of strings containing all of the item properties on an i tem.
        /// </summary>
        /// <param name="itemProperties">The list of item properties to use.</param>
        /// <returns>A list containing all of the item properties.</returns>
        public static GuiBindingList<string> BuildItemPropertyList(List<ItemProperty> itemProperties)
        {
            return Instance.BuildItemPropertyList(itemProperties);
        }

        /// <summary>
        /// Determines whether an item can be stored persistently in the database.
        /// </summary>
        /// <param name="player">The player attempting to persistently store the item.</param>
        /// <param name="item">The item being stored.</param>
        /// <returns>An error message if validation fails, otherwise an empty string if it succeeds.</returns>
        public static string CanBePersistentlyStored(uint player, uint item)
        {
            return Instance.CanBePersistentlyStored(player, item);
        }

        /// <summary>
        /// Returns the cumulative DMG value on a given item.
        /// A minimum of 1 is always returned.
        /// No checks for item type are made in this method.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>The DMG rating, or 1 if not found.</returns>
        public static int GetDMG(uint item)
        {
            return Instance.GetDMG(item);
        }

        /// <summary>
        /// Reduces an item stack by a specific amount.
        /// If there are not enough items in the stack to reduce, false will be returned.
        /// If the stack size of the item will reach 0, the item is destroyed and true will be returned.
        /// If the stack size will reach a number greater than 0, the item's stack size will be updated and true will be returned.
        /// </summary>
        /// <param name="item">The item to adjust</param>
        /// <param name="reduceBy">The amount to reduce by. Absolute value is used to determine this value.</param>
        /// <returns>true if successfully reduced or destroyed, false otherwise</returns>
        public static bool ReduceItemStack(uint item, int reduceBy)
        {
            return Instance.ReduceItemStack(item, reduceBy);
        }

        /// <summary>
        /// Returns true if <see cref="PlayerProducedItemVariable"/> is set (e.g. crafting stamped the item).
        /// </summary>
        public static bool IsPlayerProducedItem(uint item)
        {
            return Instance.IsPlayerProducedItem(item);
        }

        /// <summary>
        /// Determines if an item is a legacy item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>true if item is legacy, false otherwise</returns>
        public static bool IsLegacyItem(uint item)
        {
            return Instance.IsLegacyItem(item);
        }

        /// <summary>
        /// Retrieves the item slot of a specific item.
        /// If the item isn't equipped, InventorySlot.Invalid will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="item">The item to search for.</param>
        /// <returns>The inventory slot of the item or InventorySlot.Invalid if not equipped.</returns>
        public static InventorySlot GetItemSlot(uint creature, uint item)
        {
            return Instance.GetItemSlot(creature, item);
        }
    }
}

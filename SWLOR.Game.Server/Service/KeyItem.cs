using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the key items. The logic and state live in <see cref="IKeyItemService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class KeyItem
    {
        private static IKeyItemService Instance => FacadeAccessor.Resolve<IKeyItemService>();

        /// <summary>
        /// When the module loads, cache all key item data.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadData()
        {
            Instance.LoadData();
        }

        /// <summary>
        /// Gets a key item category's detail by its type.
        /// </summary>
        /// <param name="type">The type of key item category to retrieve.</param>
        /// <returns>A key item category detail.</returns>
        public static KeyItemCategoryAttribute GetKeyItemCategory(KeyItemCategoryType type)
        {
            return Instance.GetKeyItemCategory(type);
        }

        /// <summary>
        /// Gets a key item's detail by its type.
        /// </summary>
        /// <param name="keyItem">The type of key item to retrieve.</param>
        /// <returns>A key item detail</returns>
        public static KeyItemAttribute GetKeyItem(KeyItemType keyItem)
        {
            return Instance.GetKeyItem(keyItem);
        }

        /// <summary>
        /// Retrieves a key item type by its integer Id.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="keyItemId">The Id to search for</param>
        /// <returns>A KeyItemType matching the Id.</returns>
        public static KeyItemType GetKeyItemTypeById(int keyItemId)
        {
            return Instance.GetKeyItemTypeById(keyItemId);
        }

        /// <summary>
        /// Retrieves a key item type by its name.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>A KeyItemType matching the name.</returns>
        public static KeyItemType GetKeyItemTypeByName(string name)
        {
            return Instance.GetKeyItemTypeByName(name);
        }

        /// <summary>
        /// Retrieves all of the active key item categories.
        /// </summary>
        /// <returns>All active key item categories</returns>
        public static Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> GetActiveCategories()
        {
            return Instance.GetActiveCategories();
        }

        /// <summary>
        /// Retrieves all of the active key items contained in a specific category.
        /// </summary>
        /// <param name="category">The category to search by</param>
        /// <returns>A dictionary containing key item type and key item attribute data.</returns>
        public static Dictionary<KeyItemType, KeyItemAttribute> GetActiveKeyItemsByCategory(KeyItemCategoryType category)
        {
            return Instance.GetActiveKeyItemsByCategory(category);
        }

        /// <summary>
        /// Gives a specific key item to a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player already has the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to give.</param>
        public static void GiveKeyItem(uint player, KeyItemType keyItem)
        {
            Instance.GiveKeyItem(player, keyItem);
        }

        /// <summary>
        /// Removes a key item from a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player does not have the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to remove.</param>
        public static void RemoveKeyItem(uint player, KeyItemType keyItem)
        {
            Instance.RemoveKeyItem(player, keyItem);
        }

        /// <summary>
        /// Checks whether a player has a key item.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItem">The type of key item to check for.</param>
        /// <returns>true if the player has a key item, false otherwise</returns>
        public static bool HasKeyItem(uint player, KeyItemType keyItem)
        {
            return Instance.HasKeyItem(player, keyItem);
        }

        /// <summary>
        /// Checks whether a player has all of the specified key items.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItems">Required key items.</param>
        /// <returns>true if player has all key items, false otherwise</returns>
        public static bool HasAllKeyItems(uint player, List<KeyItemType> keyItems)
        {
            return Instance.HasAllKeyItems(player, keyItems);
        }

        /// <summary>
        /// When a placeable with a key item defined is used by a player, give it to them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnGetKeyItem)]
        public static void ObtainKeyItem()
        {
            Instance.ObtainKeyItem();
        }
    }
}

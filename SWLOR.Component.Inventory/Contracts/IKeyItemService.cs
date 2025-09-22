using SWLOR.Component.Inventory.Enums;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Inventory.Contracts
{
    /// <summary>
    /// Service for managing key items
    /// </summary>
    public interface IKeyItemService
    {
        /// <summary>
        /// Gets a key item category's detail by its type.
        /// </summary>
        /// <param name="type">The type of key item category to retrieve.</param>
        /// <returns>A key item category detail.</returns>
        KeyItemCategoryAttribute GetKeyItemCategory(KeyItemCategoryType type);

        /// <summary>
        /// Gets a key item's detail by its type.
        /// </summary>
        /// <param name="keyItem">The type of key item to retrieve.</param>
        /// <returns>A key item detail</returns>
        KeyItemAttribute GetKeyItem(KeyItemType keyItem);

        /// <summary>
        /// Retrieves a key item type by its integer Id.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="keyItemId">The Id to search for</param>
        /// <returns>A KeyItemType matching the Id.</returns>
        KeyItemType GetKeyItemTypeById(int keyItemId);

        /// <summary>
        /// Retrieves a key item type by its name.
        /// Returns KeyItemType.Invalid if not found.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>A KeyItemType matching the name.</returns>
        KeyItemType GetKeyItemTypeByName(string name);

        /// <summary>
        /// Retrieves all of the active key item categories.
        /// </summary>
        /// <returns>All active key item categories</returns>
        Dictionary<KeyItemCategoryType, KeyItemCategoryAttribute> GetActiveCategories();

        /// <summary>
        /// Retrieves all of the active key items contained in a specific category.
        /// </summary>
        /// <param name="category">The category to search by</param>
        /// <returns>A dictionary containing key item type and key item attribute data.</returns>
        Dictionary<KeyItemType, KeyItemAttribute> GetActiveKeyItemsByCategory(KeyItemCategoryType category);

        /// <summary>
        /// Gives a specific key item to a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player already has the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to give.</param>
        void GiveKeyItem(uint player, KeyItemType keyItem);

        /// <summary>
        /// Removes a key item from a player.
        /// If player is not a PC or is a DM, nothing will happen.
        /// If player does not have the key item, nothing will happen.
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="keyItem">The key item type to remove.</param>
        void RemoveKeyItem(uint player, KeyItemType keyItem);

        /// <summary>
        /// Checks whether a player has a key item.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItem">The type of key item to check for.</param>
        /// <returns>true if the player has a key item, false otherwise</returns>
        bool HasKeyItem(uint player, KeyItemType keyItem);

        /// <summary>
        /// Checks whether a player has all of the specified key items.
        /// Returns false if player is non-PC or DM.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="keyItems">Required key items.</param>
        /// <returns>true if player has all key items, false otherwise</returns>
        bool HasAllKeyItems(uint player, List<KeyItemType> keyItems);
    }
}

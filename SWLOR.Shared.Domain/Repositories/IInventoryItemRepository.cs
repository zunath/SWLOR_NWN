using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for InventoryItem entity operations.
    /// </summary>
    public interface IInventoryItemRepository
    {
        /// <summary>
        /// Gets an inventory item by its unique identifier.
        /// </summary>
        /// <param name="id">The inventory item's unique identifier</param>
        /// <returns>The inventory item if found, null otherwise</returns>
        InventoryItem GetById(string id);

        /// <summary>
        /// Gets all inventory items by storage ID.
        /// </summary>
        /// <param name="storageId">The storage ID to search for</param>
        /// <returns>Collection of inventory items in the specified storage</returns>
        IEnumerable<InventoryItem> GetByStorageId(string storageId);

        /// <summary>
        /// Gets all inventory items by storage ID and player ID.
        /// </summary>
        /// <param name="storageId">The storage ID to search for</param>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of inventory items in the specified storage owned by the player</returns>
        IEnumerable<InventoryItem> GetByStorageIdAndPlayerId(string storageId, string playerId);

        /// <summary>
        /// Gets the count of inventory items by storage ID and player ID.
        /// </summary>
        /// <param name="storageId">The storage ID to search for</param>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>The count of inventory items in the specified storage owned by the player</returns>
        long GetCountByStorageIdAndPlayerId(string storageId, string playerId);

        /// <summary>
        /// Gets all inventory items by player ID.
        /// </summary>
        /// <param name="playerId">The player ID to search for</param>
        /// <returns>Collection of inventory items owned by the specified player</returns>
        IEnumerable<InventoryItem> GetByPlayerId(string playerId);

        /// <summary>
        /// Gets all inventory items by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of inventory items with the specified name</returns>
        IEnumerable<InventoryItem> GetByName(string name);

        /// <summary>
        /// Gets all inventory items by tag.
        /// </summary>
        /// <param name="tag">The tag to search for</param>
        /// <returns>Collection of inventory items with the specified tag</returns>
        IEnumerable<InventoryItem> GetByTag(string tag);

        /// <summary>
        /// Gets all inventory items by resref.
        /// </summary>
        /// <param name="resref">The resref to search for</param>
        /// <returns>Collection of inventory items with the specified resref</returns>
        IEnumerable<InventoryItem> GetByResref(string resref);

        /// <summary>
        /// Saves an inventory item entity.
        /// </summary>
        /// <param name="inventoryItem">The inventory item to save</param>
        void Save(InventoryItem inventoryItem);

        /// <summary>
        /// Deletes an inventory item by its unique identifier.
        /// </summary>
        /// <param name="id">The inventory item's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Gets all inventory items.
        /// </summary>
        /// <returns>Collection of all inventory items</returns>
        IEnumerable<InventoryItem> GetAll();

        /// <summary>
        /// Gets the count of all inventory items.
        /// </summary>
        /// <returns>The count of all inventory items</returns>
        long GetCount();

        /// <summary>
        /// Checks if an inventory item exists by its unique identifier.
        /// </summary>
        /// <param name="id">The inventory item's unique identifier</param>
        /// <returns>True if the inventory item exists, false otherwise</returns>
        bool Exists(string id);
    }
}
